using VehiclePartsAPI.Models;
using VehiclePartsAPI.DTOs;
using VehiclePartsAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace VehiclePartsAPI.Controllers;

[ApiController]
[Route("api/sales")]
public class SalesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;

    private const decimal LoyaltyThreshold = 5000m;
    private const decimal LoyaltyDiscountRate = 0.10m;

    public SalesController(
        AppDbContext context,
        IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    // GET: api/sales
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] int? customerId)
    {
        var query = _context.SaleInvoices.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(si => si.Status == status);
        }

        if (customerId.HasValue)
        {
            query = query.Where(si => si.CustomerId == customerId.Value);
        }

        var invoices = await query
            .OrderByDescending(si => si.InvoiceDate)
            .Select(si => new SaleInvoiceSummaryDto
            {
                Id = si.Id,
                InvoiceNumber = si.InvoiceNumber,
                InvoiceDate = si.InvoiceDate,
                SubTotal = si.SubTotal,
                DiscountAmount = si.DiscountAmount,
                TotalAmount = si.TotalAmount,
                LoyaltyDiscountApplied = si.LoyaltyDiscountApplied,
                Status = si.Status,
                PaymentMethod = si.PaymentMethod
            })
            .ToListAsync();

        return Ok(invoices);
    }


    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var invoice = await _context.SaleInvoices
            .Include(si => si.Customer)
            .Include(si => si.Items)
                .ThenInclude(i => i.Part)
            .FirstOrDefaultAsync(si => si.Id == id);

        if (invoice == null)
        {
            return NotFound(new
            {
                message = $"Sales invoice with ID {id} not found."
            });
        }

        return Ok(MapToDetailDto(invoice));
    }


    [HttpGet("customer/{customerId:int}")]
    public async Task<IActionResult> GetByCustomer(int customerId)
    {
        var customerExists = await _context.Customers
            .AnyAsync(c => c.Id == customerId);

        if (!customerExists)
        {
            return NotFound(new
            {
                message = $"Customer with ID {customerId} not found."
            });
        }

        var invoices = await _context.SaleInvoices
            .Where(si => si.CustomerId == customerId)
            .OrderByDescending(si => si.InvoiceDate)
            .Select(si => new SaleInvoiceSummaryDto
            {
                Id = si.Id,
                InvoiceNumber = si.InvoiceNumber,
                InvoiceDate = si.InvoiceDate,
                SubTotal = si.SubTotal,
                DiscountAmount = si.DiscountAmount,
                TotalAmount = si.TotalAmount,
                LoyaltyDiscountApplied = si.LoyaltyDiscountApplied,
                Status = si.Status,
                PaymentMethod = si.PaymentMethod
            })
            .ToListAsync();

        return Ok(invoices);
    }

    // POST: api/sales
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSaleInvoiceDto dto)
    {
        var customer = await _context.Customers
            .FindAsync(dto.CustomerId);

        if (customer == null)
        {
            return NotFound(new
            {
                message = $"Customer with ID {dto.CustomerId} not found."
            });
        }

        var partIds = dto.Items
            .Select(i => i.PartId)
            .Distinct()
            .ToList();

        var parts = await _context.Parts
            .Where(p => partIds.Contains(p.Id))
            .ToListAsync();

        if (parts.Count != partIds.Count)
        {
            var missing = partIds.Except(parts.Select(p => p.Id));

            return NotFound(new
            {
                message = $"Parts not found: {string.Join(", ", missing)}"
            });
        }

        var partDict = parts.ToDictionary(p => p.Id);

        var stockErrors = new List<string>();

        foreach (var item in dto.Items)
        {
            var part = partDict[item.PartId];

            if (part.StockQty < item.Quantity)
            {
                stockErrors.Add(
                    $"'{part.Name}' has only {part.StockQty} unit(s) in stock."
                );
            }
        }

        if (stockErrors.Any())
        {
            return BadRequest(new
            {
                message = "Insufficient stock.",
                errors = stockErrors
            });
        }

        var saleItems = dto.Items.Select(i => new SaleInvoiceItem
        {
            PartId = i.PartId,
            Quantity = i.Quantity,
            UnitPrice = partDict[i.PartId].SellingPrice
        }).ToList();

        var subTotal = saleItems.Sum(i => i.Quantity * i.UnitPrice);

        var loyaltyApplied = subTotal > LoyaltyThreshold;

        var discount = loyaltyApplied
            ? Math.Round(subTotal * LoyaltyDiscountRate, 2)
            : 0m;

        var totalAmount = subTotal - discount;

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

            Status = dto.PaymentMethod == "Credit"
                ? "Credit"
                : "Paid",

            Items = saleItems
        };

        foreach (var item in dto.Items)
        {
            partDict[item.PartId].StockQty -= item.Quantity;
            partDict[item.PartId].UpdatedAt = DateTime.UtcNow;
        }

        _context.SaleInvoices.Add(invoice);

        await _context.SaveChangesAsync();

        var created = await _context.SaleInvoices
            .Include(si => si.Customer)
            .Include(si => si.Items)
                .ThenInclude(i => i.Part)
            .FirstAsync(si => si.Id == invoice.Id);

        // Auto send invoice email
        _ = Task.Run(() =>
            _emailService.SendSaleInvoiceAsync(created));

        return CreatedAtAction(
            nameof(GetById),
            new { id = invoice.Id },
            MapToDetailDto(created)
        );
    }

    // PATCH: api/sales/{id}/status
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateSaleStatusDto dto)
    {
        var allowed = new[] { "Paid", "Cancelled" };

        if (!allowed.Contains(dto.Status))
        {
            return BadRequest(new
            {
                message = "Status must be 'Paid' or 'Cancelled'."
            });
        }

        var invoice = await _context.SaleInvoices
            .Include(si => si.Items)
            .FirstOrDefaultAsync(si => si.Id == id);

        if (invoice == null)
        {
            return NotFound(new
            {
                message = $"Sales invoice with ID {id} not found."
            });
        }

        if (invoice.Status == dto.Status)
        {
            return BadRequest(new
            {
                message = $"Invoice is already '{dto.Status}'."
            });
        }

        if (invoice.Status == "Cancelled")
        {
            return BadRequest(new
            {
                message = "Cannot modify cancelled invoice."
            });
        }

        // Restore stock if cancelled
        if (dto.Status == "Cancelled")
        {
            var partIds = invoice.Items
                .Select(i => i.PartId)
                .ToList();

            var parts = await _context.Parts
                .Where(p => partIds.Contains(p.Id))
                .ToListAsync();

            var partDict = parts.ToDictionary(p => p.Id);

            foreach (var item in invoice.Items)
            {
                partDict[item.PartId].StockQty += item.Quantity;
                partDict[item.PartId].UpdatedAt = DateTime.UtcNow;
            }
        }

        invoice.Status = dto.Status;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id:int}/send-email")]
    public async Task<IActionResult> SendEmail(int id)
    {
        var invoice = await _context.SaleInvoices
            .Include(si => si.Customer)
            .Include(si => si.Items)
                .ThenInclude(i => i.Part)
            .FirstOrDefaultAsync(si => si.Id == id);

        if (invoice == null)
        {
            return NotFound(new
            {
                message = $"Sales invoice with ID {id} not found."
            });
        }

        if (string.IsNullOrWhiteSpace(invoice.Customer?.Email))
        {
            return BadRequest(new
            {
                message = "Customer email not found."
            });
        }

        await _emailService.SendSaleInvoiceAsync(invoice);

        return Ok(new
        {
            message =
                $"Invoice {invoice.InvoiceNumber} emailed successfully."
        });
    }

    //  Helper 

    private static SaleInvoiceDetailDto MapToDetailDto(SaleInvoice si)
    {
        return new SaleInvoiceDetailDto
        {
            Id = si.Id,

            InvoiceNumber = si.InvoiceNumber,

            InvoiceDate = si.InvoiceDate,

            SubTotal = si.SubTotal,

            DiscountAmount = si.DiscountAmount,

            TotalAmount = si.TotalAmount,

            LoyaltyDiscountApplied =
                si.LoyaltyDiscountApplied,

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

            Items = si.Items?.Select(i => new SaleInvoiceItemDto
            {
                PartId = i.PartId,

                PartName =
                    i.Part?.Name ?? "Unknown Part",

                PartNumber =
                    i.Part?.PartNumber ?? "N/A",

                Quantity = i.Quantity,

                UnitPrice = i.UnitPrice,

                LineTotal = i.Quantity * i.UnitPrice

            }).ToList() ?? new List<SaleInvoiceItemDto>()
        };
    }
}
