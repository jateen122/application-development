using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsAPI.Models;
using VehiclePartsAPI.DTOs;

[ApiController]
[Route("api/purchase-invoices")]
public class PurchaseInvoicesController : ControllerBase
{
    private readonly AppDbContext _context;

    public PurchaseInvoicesController(AppDbContext context) => _context = context;


    // Returns a summary list of all purchase invoices.
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int? vendorId)
    {
        var query = _context.PurchaseInvoices.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(pi => pi.Status == status);

        if (vendorId.HasValue)
            query = query.Where(pi => pi.VendorId == vendorId.Value);

        var invoices = await query
            .OrderByDescending(pi => pi.InvoiceDate)
            .Select(pi => new PurchaseInvoiceDto
            {
                Id = pi.Id,
                InvoiceNumber = pi.InvoiceNumber,
                InvoiceDate = pi.InvoiceDate,
                TotalAmount = pi.TotalAmount,
                Status = pi.Status,
                VendorId = pi.VendorId,
                VendorName = pi.Vendor.Name,
                ItemCount = pi.Items.Count
            })
            .ToListAsync();

        return Ok(invoices);
    }

    // GET /api/purchase-invoices/{id}
    // Returns full invoice details including line items.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var invoice = await _context.PurchaseInvoices
            .Include(pi => pi.Vendor)
            .Include(pi => pi.Items)
                .ThenInclude(i => i.Part)
            .FirstOrDefaultAsync(pi => pi.Id == id);

        if (invoice == null)
            return NotFound(new { message = $"Purchase invoice with ID {id} not found." });

        return Ok(MapToDetailDto(invoice));
    }

    // POST /api/purchase-invoices
    // Creates a new purchase invoice.
    // Stock is updated immediately when the invoice is created (or on status = "Received").
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseInvoiceDto dto)
    {
        // Validate vendor exists
        var vendorExists = await _context.Vendors.AnyAsync(v => v.Id == dto.VendorId);
        if (!vendorExists)
            return NotFound(new { message = $"Vendor with ID {dto.VendorId} not found." });

        // Validate all parts exist
        var partIds = dto.Items.Select(i => i.PartId).Distinct().ToList();
        var foundParts = await _context.Parts
            .Where(p => partIds.Contains(p.Id))
            .ToListAsync();

        if (foundParts.Count != partIds.Count)
        {
            var missing = partIds.Except(foundParts.Select(p => p.Id));
            return NotFound(new { message = $"Parts not found: {string.Join(", ", missing)}" });
        }

        // Generate a unique invoice number: PI-YYYY-NNNN
        var year = DateTime.UtcNow.Year;
        var count = await _context.PurchaseInvoices.CountAsync() + 1;
        var invoiceNumber = $"PI-{year}-{count:D4}";

        // Build invoice items and calculate total
        var items = dto.Items.Select(i => new PurchaseInvoiceItem
        {
            PartId = i.PartId,
            Quantity = i.Quantity,
            UnitCost = i.UnitCost
        }).ToList();

        var total = items.Sum(i => i.Quantity * i.UnitCost);

        var invoice = new PurchaseInvoice
        {
            InvoiceNumber = invoiceNumber,
            InvoiceDate = dto.InvoiceDate,
            VendorId = dto.VendorId,
            TotalAmount = total,
            Status = "Received",    // Auto-receive: stock is updated on creation
            Items = items
        };

        // Update stock quantities immediately (invoice treated as "Received" on creation)
        var partDict = foundParts.ToDictionary(p => p.Id);
        foreach (var item in dto.Items)
        {
            partDict[item.PartId].StockQty += item.Quantity;
            partDict[item.PartId].UpdatedAt = DateTime.UtcNow;
        }

        _context.PurchaseInvoices.Add(invoice);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, new PurchaseInvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            TotalAmount = invoice.TotalAmount,
            Status = invoice.Status,
            VendorId = invoice.VendorId,
            VendorName = (await _context.Vendors.FindAsync(invoice.VendorId))!.Name,
            ItemCount = invoice.Items.Count
        });
    }

    // PATCH /api/purchase-invoices/{id}/status
    // Update invoice status (e.g. Pending → Received → Cancelled).
    // If moving to "Received", stock quantities are incremented.
    // If moving to "Cancelled", stock already added is reversed if previously received.
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdatePurchaseInvoiceStatusDto dto)
    {
        var allowed = new[] { "Pending", "Received", "Cancelled" };
        if (!allowed.Contains(dto.Status))
            return BadRequest(new { message = $"Invalid status. Allowed values: {string.Join(", ", allowed)}" });

        var invoice = await _context.PurchaseInvoices
            .Include(pi => pi.Items)
            .FirstOrDefaultAsync(pi => pi.Id == id);

        if (invoice == null)
            return NotFound(new { message = $"Purchase invoice with ID {id} not found." });

        var previousStatus = invoice.Status;

        if (previousStatus == dto.Status)
            return BadRequest(new { message = $"Invoice is already in '{dto.Status}' status." });

        // Apply stock changes based on transition
        if (dto.Status == "Received" && previousStatus == "Pending")
        {
            // Add stock
            var partIds = invoice.Items.Select(i => i.PartId).ToList();
            var parts = await _context.Parts.Where(p => partIds.Contains(p.Id)).ToListAsync();
            var partDict = parts.ToDictionary(p => p.Id);

            foreach (var item in invoice.Items)
            {
                partDict[item.PartId].StockQty += item.Quantity;
                partDict[item.PartId].UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (dto.Status == "Cancelled" && previousStatus == "Received")
        {
            // Reverse stock
            var partIds = invoice.Items.Select(i => i.PartId).ToList();
            var parts = await _context.Parts.Where(p => partIds.Contains(p.Id)).ToListAsync();
            var partDict = parts.ToDictionary(p => p.Id);

            foreach (var item in invoice.Items)
            {
                partDict[item.PartId].StockQty = Math.Max(0, partDict[item.PartId].StockQty - item.Quantity);
                partDict[item.PartId].UpdatedAt = DateTime.UtcNow;
            }
        }

        invoice.Status = dto.Status;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/purchase-invoices/{id}
    // Only "Pending" invoices can be deleted.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var invoice = await _context.PurchaseInvoices
            .Include(pi => pi.Items)
            .FirstOrDefaultAsync(pi => pi.Id == id);

        if (invoice == null)
            return NotFound(new { message = $"Purchase invoice with ID {id} not found." });

        if (invoice.Status == "Received")
            return BadRequest(new { message = "Cannot delete a received invoice. Cancel it first." });

        _context.PurchaseInvoices.Remove(invoice);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    //  Helpers 
    private static PurchaseInvoiceDetailDto MapToDetailDto(PurchaseInvoice pi) => new()
    {
        Id = pi.Id,
        InvoiceNumber = pi.InvoiceNumber,
        InvoiceDate = pi.InvoiceDate,
        TotalAmount = pi.TotalAmount,
        Status = pi.Status,
        Vendor = new VendorDto
        {
            Id = pi.Vendor.Id,
            Name = pi.Vendor.Name,
            ContactPerson = pi.Vendor.ContactPerson,
            Email = pi.Vendor.Email,
            Phone = pi.Vendor.Phone,
            Address = pi.Vendor.Address,
            CreatedAt = pi.Vendor.CreatedAt
        },
        Items = pi.Items.Select(i => new PurchaseInvoiceItemDto
        {
            PartId = i.PartId,
            PartName = i.Part.Name,
            PartNumber = i.Part.PartNumber,
            Quantity = i.Quantity,
            UnitCost = i.UnitCost,
            LineTotal = i.Quantity * i.UnitCost
        }).ToList()
    };
}
