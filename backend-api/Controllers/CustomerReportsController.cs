using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsAPI.Data;
using VehiclePartsAPI.DTOs;

[ApiController]
[Route("api/customer-reports")]
public class CustomerReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public CustomerReportsController(AppDbContext context) => _context = context;


    [HttpGet("top-spenders")]
    public async Task<IActionResult> TopSpenders([FromQuery] int limit = 20)
    {
        var result = await _context.Customers
            .Select(c => new CustomerSpendDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Phone = c.Phone,
                TotalSpend = c.SaleInvoices
                    .Where(si => si.Status != "Cancelled")
                    .Sum(si => (decimal?)si.TotalAmount) ?? 0,
                InvoiceCount = c.SaleInvoices.Count(si => si.Status != "Cancelled"),
                LoyaltyInvoices = c.SaleInvoices.Count(si => si.LoyaltyDiscountApplied),
                LastPurchase = c.SaleInvoices
                    .Where(si => si.Status != "Cancelled")
                    .OrderByDescending(si => si.InvoiceDate)
                    .Select(si => (DateTime?)si.InvoiceDate)
                    .FirstOrDefault()
            })
            .Where(c => c.TotalSpend > 0)
            .OrderByDescending(c => c.TotalSpend)
            .Take(limit)
            .ToListAsync();

        return Ok(result);
    }


    [HttpGet("regulars")]
    public async Task<IActionResult> Regulars([FromQuery] int minPurchases = 3)
    {
        var result = await _context.Customers
            .Select(c => new CustomerSpendDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Phone = c.Phone,
                TotalSpend = c.SaleInvoices
                    .Where(si => si.Status != "Cancelled")
                    .Sum(si => (decimal?)si.TotalAmount) ?? 0,
                InvoiceCount = c.SaleInvoices.Count(si => si.Status != "Cancelled"),
                LoyaltyInvoices = c.SaleInvoices.Count(si => si.LoyaltyDiscountApplied),
                LastPurchase = c.SaleInvoices
                    .Where(si => si.Status != "Cancelled")
                    .OrderByDescending(si => si.InvoiceDate)
                    .Select(si => (DateTime?)si.InvoiceDate)
                    .FirstOrDefault()
            })
            .Where(c => c.InvoiceCount >= minPurchases)
            .OrderByDescending(c => c.InvoiceCount)
            .ToListAsync();

        return Ok(result);
    }


    [HttpGet("pending-credits")]
    public async Task<IActionResult> PendingCredits()
    {
        var result = await _context.Customers
            .Where(c => c.SaleInvoices.Any(si => si.Status == "Credit"))
            .Select(c => new CustomerCreditDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Phone = c.Phone,
                CreditInvoiceCount = c.SaleInvoices.Count(si => si.Status == "Credit"),
                TotalCreditAmount = c.SaleInvoices
                    .Where(si => si.Status == "Credit")
                    .Sum(si => (decimal?)si.TotalAmount) ?? 0,
                OldestCreditDate = c.SaleInvoices
                    .Where(si => si.Status == "Credit")
                    .OrderBy(si => si.InvoiceDate)
                    .Select(si => (DateTime?)si.InvoiceDate)
                    .FirstOrDefault()
            })
            .OrderByDescending(c => c.TotalCreditAmount)
            .ToListAsync();

        return Ok(result);
    }

    [HttpGet("overdue-credits")]
    public async Task<IActionResult> OverdueCredits()
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);

        var result = await _context.Customers
            .Where(c => c.SaleInvoices.Any(si => si.Status == "Credit" && si.InvoiceDate <= cutoff))
            .Select(c => new CustomerCreditDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Phone = c.Phone,
                CreditInvoiceCount = c.SaleInvoices
                    .Count(si => si.Status == "Credit" && si.InvoiceDate <= cutoff),
                TotalCreditAmount = c.SaleInvoices
                    .Where(si => si.Status == "Credit" && si.InvoiceDate <= cutoff)
                    .Sum(si => (decimal?)si.TotalAmount) ?? 0,
                OldestCreditDate = c.SaleInvoices
                    .Where(si => si.Status == "Credit")
                    .OrderBy(si => si.InvoiceDate)
                    .Select(si => (DateTime?)si.InvoiceDate)
                    .FirstOrDefault()
            })
            .OrderBy(c => c.OldestCreditDate)
            .ToListAsync();

        return Ok(result);
    }


    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Search query 'q' is required." });

        var term = q.Trim().ToLower();

        // Try to parse as numeric ID
        var isNumeric = int.TryParse(q.Trim(), out var numericId);

        // Search customers by name / email / phone / ID
        var customers = await _context.Customers
            .Include(c => c.Vehicles)
            .Where(c =>
                c.FirstName.ToLower().Contains(term) ||
                c.LastName.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term) ||
                c.Phone.Contains(term) ||
                (isNumeric && c.Id == numericId) ||
                c.Vehicles.Any(v => v.VehicleNumber.ToLower().Contains(term)))
            .Select(c => new CustomerSearchResultDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Phone = c.Phone,
                Address = c.Address,
                CreatedAt = c.CreatedAt,
                MatchedVehicles = c.Vehicles
                    .Where(v => v.VehicleNumber.ToLower().Contains(term))
                    .Select(v => v.VehicleNumber)
                    .ToList()
            })
            .ToListAsync();

        return Ok(customers);
    }
}
