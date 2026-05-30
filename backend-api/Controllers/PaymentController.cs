using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsAPI.Data;
using VehiclePartsAPI.DTOs;
using VehiclePartsAPI.Models;
using System.Text.Json;

/// <summary>
/// Khalti Payment Integration (Sandbox / Production)
///
/// Endpoints:
///   POST /api/payment/initiate       — Create Khalti payment, returns payment_url
///   GET  /api/payment/callback       — Khalti redirects here after payment
///   POST /api/payment/lookup         — Verify payment status via pidx
///   GET  /api/payment/parts          — Public parts catalogue for customer shop
/// </summary>
[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _http;

    // Khalti sandbox base URL — change to https://khalti.com/api/v2/ for production
    private const string KhaltiBaseUrl = "https://dev.khalti.com/api/v2/";

    public PaymentController(AppDbContext context, IConfiguration config, IHttpClientFactory http)
    {
        _context = context;
        _config  = config;
        _http    = http;
    }

    // ── GET /api/payment/parts ────────────────────────────────
    // Public catalogue for customer shop — no auth needed
    [HttpGet("parts")]
    public async Task<IActionResult> GetParts([FromQuery] string? category, [FromQuery] string? search)
    {
        var query = _context.Parts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.PartNumber.Contains(search));

        var parts = await query
            .Where(p => p.StockQty > 0)
            .OrderBy(p => p.Category).ThenBy(p => p.Name)
            .Select(p => new PartDto
            {
                Id           = p.Id,
                Name         = p.Name,
                PartNumber   = p.PartNumber,
                Description  = p.Description,
                Category     = p.Category,
                CostPrice    = p.CostPrice,
                SellingPrice = p.SellingPrice,
                StockQty     = p.StockQty,
                UpdatedAt    = p.UpdatedAt
            })
            .ToListAsync();

        return Ok(parts);
    }

    // ── POST /api/payment/initiate ────────────────────────────
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate([FromBody] KhaltiInitiateDto dto)
    {
        // Validate customer
        var customer = await _context.Customers.FindAsync(dto.CustomerId);
        if (customer == null)
            return NotFound(new { message = "Customer not found." });

        // Validate parts & stock
        var partIds  = dto.Items.Select(i => i.PartId).Distinct().ToList();
        var parts    = await _context.Parts.Where(p => partIds.Contains(p.Id)).ToListAsync();

        if (parts.Count != partIds.Count)
            return NotFound(new { message = "One or more parts not found." });

        var partDict    = parts.ToDictionary(p => p.Id);
        var stockErrors = new List<string>();

        foreach (var item in dto.Items)
        {
            var part = partDict[item.PartId];
            if (part.StockQty < item.Quantity)
                stockErrors.Add($"'{part.Name}' only has {part.StockQty} unit(s) in stock.");
        }

        if (stockErrors.Any())
            return BadRequest(new { message = "Insufficient stock.", errors = stockErrors });

        // Calculate totals
        var saleItems = dto.Items.Select(i => new SaleInvoiceItem
        {
            PartId    = i.PartId,
            Quantity  = i.Quantity,
            UnitPrice = partDict[i.PartId].SellingPrice
        }).ToList();

        var subTotal       = saleItems.Sum(i => i.Quantity * i.UnitPrice);
        var loyaltyApplied = subTotal > 5000m;
        var discount       = loyaltyApplied ? Math.Round(subTotal * 0.10m, 2) : 0m;
        var totalAmount    = subTotal - discount;

        // Generate invoice number
        var year    = DateTime.UtcNow.Year;
        var count   = await _context.SaleInvoices.CountAsync() + 1;
        var invoiceNumber = $"SI-{year}-{count:D4}";

        var purchaseOrderId = $"VPP-{invoiceNumber}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        // Create invoice in PENDING state (stock NOT deducted yet)
        var invoice = new SaleInvoice
        {
            InvoiceNumber          = invoiceNumber,
            InvoiceDate            = DateTime.UtcNow,
            CustomerId             = dto.CustomerId,
            SubTotal               = subTotal,
            DiscountAmount         = discount,
            TotalAmount            = totalAmount,
            LoyaltyDiscountApplied = loyaltyApplied,
            PaymentMethod          = "Khalti",
            Status                 = "Pending",
            Items                  = saleItems
        };

        _context.SaleInvoices.Add(invoice);
        await _context.SaveChangesAsync();

        // Build callback URL — points back to this API
        var apiBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var returnUrl  = $"{apiBaseUrl}/api/payment/callback?invoiceId={invoice.Id}";

        // Determine the frontend URL for final redirect
        // Use the websiteUrl from the request (sent by the frontend), fallback to origin header
        var frontendOrigin = dto.WebsiteUrl?.TrimEnd('/') ?? GetFrontendOrigin();

        var amountInPaisa = (int)(totalAmount * 100);

        var khaltiPayload = new
        {
            return_url          = returnUrl,
            website_url         = frontendOrigin,
            amount              = amountInPaisa,
            purchase_order_id   = purchaseOrderId,
            purchase_order_name = $"VehicleParts Order {invoiceNumber}",
            customer_info = new
            {
                name  = $"{customer.FirstName} {customer.LastName}",
                email = customer.Email,
                phone = customer.Phone
            },
            product_details = dto.Items.Select(i => new
            {
                identity    = partDict[i.PartId].PartNumber,
                name        = partDict[i.PartId].Name,
                total_price = (int)(i.Quantity * partDict[i.PartId].SellingPrice * 100),
                quantity    = i.Quantity,
                unit_price  = (int)(partDict[i.PartId].SellingPrice * 100)
            }).ToList()
        };

        // Call Khalti initiate API
        var secretKey = _config["ExternalServices:KhaltiSecretKey"]
                        ?? "live_secret_key_68791341fdd94846a146f7166456030";
        var client    = _http.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Key {secretKey}");

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(khaltiPayload),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        HttpResponseMessage khaltiResponse;
        try
        {
            khaltiResponse = await client.PostAsync($"{KhaltiBaseUrl}epayment/initiate/", jsonContent);
        }
        catch (Exception ex)
        {
            _context.SaleInvoices.Remove(invoice);
            await _context.SaveChangesAsync();
            return StatusCode(503, new { message = $"Cannot reach Khalti: {ex.Message}" });
        }

        var responseBody = await khaltiResponse.Content.ReadAsStringAsync();

        if (!khaltiResponse.IsSuccessStatusCode)
        {
            _context.SaleInvoices.Remove(invoice);
            await _context.SaveChangesAsync();
            return BadRequest(new { message = "Khalti initiation failed.", detail = responseBody });
        }

        var khaltiResult = JsonSerializer.Deserialize<KhaltiInitiateResponse>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        invoice.KhaltiPidx = khaltiResult!.Pidx;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            paymentUrl   = khaltiResult.PaymentUrl,
            pidx         = khaltiResult.Pidx,
            invoiceId    = invoice.Id,
            invoiceNumber,
            totalAmount,
            loyaltyApplied
        });
    }

    // ── GET /api/payment/callback ─────────────────────────────
    // Khalti redirects here after payment
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] int    invoiceId,
        [FromQuery] string pidx,
        [FromQuery] string status,
        [FromQuery] string? transaction_id,
        [FromQuery] string? purchase_order_id)
    {
        var frontendOrigin = GetFrontendOrigin();

        var invoice = await _context.SaleInvoices
            .Include(si => si.Items)
            .FirstOrDefaultAsync(si => si.Id == invoiceId);

        if (invoice == null)
            return Redirect($"{frontendOrigin}/portal-history.html?payment=error&msg=Invoice+not+found");

        if (status == "User canceled")
        {
            _context.SaleInvoices.Remove(invoice);
            await _context.SaveChangesAsync();
            return Redirect($"{frontendOrigin}/portal-shop.html?payment=cancelled");
        }

        // Verify with Khalti lookup
        var secretKey = _config["ExternalServices:KhaltiSecretKey"]
                        ?? "live_secret_key_68791341fdd94846a146f7166456030";
        var client    = _http.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Key {secretKey}");

        var lookupContent = new StringContent(
            JsonSerializer.Serialize(new { pidx }),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        string lookupStatus = "Unknown";
        try
        {
            var lookupResponse = await client.PostAsync($"{KhaltiBaseUrl}epayment/lookup/", lookupContent);
            var lookupBody     = await lookupResponse.Content.ReadAsStringAsync();
            var lookupResult   = JsonSerializer.Deserialize<KhaltiLookupResponse>(
                lookupBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            lookupStatus = lookupResult?.Status ?? "Unknown";
        }
        catch
        {
            lookupStatus = "LookupFailed";
        }

        if (lookupStatus == "Completed")
        {
            var partIds  = invoice.Items.Select(i => i.PartId).ToList();
            var parts    = await _context.Parts.Where(p => partIds.Contains(p.Id)).ToListAsync();
            var partDict = parts.ToDictionary(p => p.Id);

            foreach (var item in invoice.Items)
            {
                partDict[item.PartId].StockQty  -= item.Quantity;
                partDict[item.PartId].UpdatedAt  = DateTime.UtcNow;
            }

            invoice.Status = "Paid";
            invoice.KhaltiTransactionId = transaction_id;
            await _context.SaveChangesAsync();

            return Redirect($"{frontendOrigin}/portal-history.html?payment=success&invoice={invoice.InvoiceNumber}");
        }
        else
        {
            _context.SaleInvoices.Remove(invoice);
            await _context.SaveChangesAsync();
            return Redirect($"{frontendOrigin}/portal-shop.html?payment=failed&status={lookupStatus}");
        }
    }

    // ── POST /api/payment/lookup ──────────────────────────────
    [HttpPost("lookup")]
    public async Task<IActionResult> Lookup([FromBody] KhaltiLookupDto dto)
    {
        var secretKey = _config["ExternalServices:KhaltiSecretKey"]
                        ?? "live_secret_key_68791341fdd94846a146f7166456030";
        var client    = _http.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Key {secretKey}");

        var content = new StringContent(
            JsonSerializer.Serialize(new { pidx = dto.Pidx }),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        var response = await client.PostAsync($"{KhaltiBaseUrl}epayment/lookup/", content);
        var body     = await response.Content.ReadAsStringAsync();

        return Content(body, "application/json");
    }

    // ── Helper: detect frontend origin from Referer or Origin header ──
    private string GetFrontendOrigin()
    {
        // Try the Origin header first (set by browsers on cross-origin requests)
        var origin = Request.Headers["Origin"].ToString();
        if (!string.IsNullOrWhiteSpace(origin))
            return origin.TrimEnd('/');

        // Try Referer
        var referer = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refUri))
            return $"{refUri.Scheme}://{refUri.Authority}";

        // Fallback — same host, assume frontend is served from root
        return $"{Request.Scheme}://{Request.Host}";
    }
}
