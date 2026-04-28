using VehiclePartsAPI.Models;
using VehiclePartsAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Feature 7 — Staff can sell vehicle parts and create sales invoices.
/// Business rules:
///   - Stock is deducted when a sale is created.
///   - 10% loyalty discount applied automatically if subtotal exceeds 5000.
///   - Invoice number generated as SI-YYYY-NNNN.
///
/// Endpoints:
///   GET  /api/sales
///   GET  /api/sales/{id}
///   POST /api/sales
///   GET  /api/sales/customer/{customerId}  — purchase history for a customer
/// </summary>
[ApiController]
[Route("api/sales")]
public class SalesController : ControllerBase
{
    private readonly AppDbContext _context;
    private const decimal LoyaltyThreshold = 5000m;
    private const decimal LoyaltyDiscountRate = 0.10m;

    public SalesController(AppDbContext context) => _context = context;

    // GET /api/sales
    // Returns all sales invoices ordered newest first.
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int? customerId)
    {
        var query = _context.SaleInvoices.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(si => si.Status == status);

        if (customerId.HasValue)
            query = query.Where(si => si.CustomerId == customerId.Value);

        var invoices = await query
            .OrderByDescending(si => si.InvoiceDate)
            .Select(si => new SaleInvoiceSummaryDto
            {
                Id = si.Id,
                InvoiceNumber = si.InvoiceNumber,
                InvoiceDate = si.InvoiceDate,
                TotalAmount = si.TotalAmount,
                LoyaltyDiscountApplied = si.LoyaltyDiscountApplied,
                Status = si.Status,
                PaymentMethod = si.PaymentMethod
            })
            .ToListAsync();

        return Ok(invoices);
    }

    // GET /api/sales/{id}
    // Returns a full sales invoice with line items and customer info.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var invoice = await _context.SaleInvoices
            .Include(si => si.Customer)
            .Include(si => si.Items)
                .ThenInclude(i => i.Part)
            .FirstOrDefaultAsync(si => si.Id == id);

        if (invoice == null)
            return NotFound(new { message = $"Sales invoice with ID {id} not found." });

        return Ok(MapToDetailDto(invoice));
    }

    // GET /api/sales/customer/{customerId}
    // Returns purchase history for a specific customer (Feature 14 support).
    [HttpGet("customer/{customerId:int}")]
    public async Task<IActionResult> GetByCustomer(int customerId)
    {
        var customerExists = await _context.Customers.AnyAsync(c => c.Id == customerId);
        if (!customerExists)
            return NotFound(new { message = $"Customer with ID {customerId} not found." });

        var invoices = await _context.SaleInvoices
            .Where(si => si.CustomerId == customerId)
            .Include(si => si.Items).ThenInclude(i => i.Part)
            .OrderByDescending(si => si.InvoiceDate)
            .ToListAsync();

        return Ok(invoices.Select(MapToDetailDto));
    }

    // POST /api/sales
    // Creates a new sales invoice. Deducts stock. Applies loyalty discount if applicable.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSaleInvoiceDto dto)
    {
        // Validate customer exists
        var customer = await _context.Customers.FindAsync(dto.CustomerId);
        if (customer == null)
            return NotFound(new { message = $"Customer with ID {dto.CustomerId} not found." });

        // Load all requested parts in one query
        var partIds = dto.Items.Select(i => i.PartId).Distinct().ToList();
        var parts = await _context.Parts
            .Where(p => partIds.Contains(p.Id))
            .ToListAsync();

        if (parts.Count != partIds.Count)
        {
            var missing = partIds.Except(parts.Select(p => p.Id));
            return NotFound(new { message = $"Parts not found: {string.Join(", ", missing)}" });
        }

        var partDict = parts.ToDictionary(p => p.Id);

        // Validate stock availability for each item
        var stockErrors = new List<string>();
        foreach (var item in dto.Items)
        {
            var part = partDict[item.PartId];
            if (part.StockQty < item.Quantity)
                stockErrors.Add($"'{part.Name}' has only {part.StockQty} unit(s) in stock (requested: {item.Quantity}).");
        }

        if (stockErrors.Any())
            return BadRequest(new { message = "Insufficient stock.", errors = stockErrors });

        // Build invoice items and calculate subtotal
        var saleItems = dto.Items.Select(i => new SaleInvoiceItem
        {
            PartId = i.PartId,
            Quantity = i.Quantity,
            UnitPrice = partDict[i.PartId].SellingPrice
        }).ToList();

        var subTotal = saleItems.Sum(i => i.Quantity * i.UnitPrice);

        // Apply loyalty discount if subtotal > 5000
        var loyaltyApplied = subTotal > LoyaltyThreshold;
        var discount = loyaltyApplied ? Math.Round(subTotal * LoyaltyDiscountRate, 2) : 0m;
        var totalAmount = subTotal - discount;

        // Generate invoice number: SI-YYYY-NNNN
        var year = DateTime.UtcNow.Year;
        var count = await _context.SaleInvoices.CountAsync() + 1;
        var invoiceNumber = $"SI-{year}-{count:D4}";

        var invoice = new SaleInvoice
        {
            InvoiceNumber = invoiceNumber,
            InvoiceDate = DateTime.UtcNow,
            CustomerId = dto.CustomerId,
            SubTotal = subTotal,
            DiscountAmount = discount,
            TotalAmount = totalAmount,
            LoyaltyDiscountApplied = loyaltyApplied,
            PaymentMethod = dto.PaymentMethod,
            Status = dto.PaymentMethod == "Credit" ? "Credit" : "Paid",
            Items = saleItems
        };

        // Deduct stock for each part sold
        foreach (var item in dto.Items)
        {
            partDict[item.PartId].StockQty -= item.Quantity;
            partDict[item.PartId].UpdatedAt = DateTime.UtcNow;
        }

        _context.SaleInvoices.Add(invoice);
        await _context.SaveChangesAsync();

        // Reload with navigation properties for response
        var created = await _context.SaleInvoices
            .Include(si => si.Customer)
            .Include(si => si.Items).ThenInclude(i => i.Part)
            .FirstAsync(si => si.Id == invoice.Id);

        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, MapToDetailDto(created));
    }

    // ── Helpers ───────────────────────────────────────────────
    private static SaleInvoiceDetailDto MapToDetailDto(SaleInvoice si) => new()
    {
        Id = si.Id,
        InvoiceNumber = si.InvoiceNumber,
        InvoiceDate = si.InvoiceDate,
        SubTotal = si.SubTotal,
        DiscountAmount = si.DiscountAmount,
        TotalAmount = si.TotalAmount,
        LoyaltyDiscountApplied = si.LoyaltyDiscountApplied,
        PaymentMethod = si.PaymentMethod,
        Status = si.Status,
        Customer = new CustomerDto
        {
            Id = si.Customer.Id,
            FirstName = si.Customer.FirstName,
            LastName = si.Customer.LastName,
            Email = si.Customer.Email,
            Phone = si.Customer.Phone,
            Address = si.Customer.Address,
            CreatedAt = si.Customer.CreatedAt
        },
        Items = si.Items.Select(i => new SaleInvoiceItemDto
        {
            PartId = i.PartId,
            PartName = i.Part.Name,
            PartNumber = i.Part.PartNumber,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            LineTotal = i.Quantity * i.UnitPrice
        }).ToList()
    };
}
