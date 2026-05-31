using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsAPI.DTOs;
using VehiclePartsAPI.Models;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context) => _context = context;


[HttpGet("daily")]
public async Task<IActionResult> Daily([FromQuery] DateTime? date)
{
    try
    {
        var reportDate = DateTime.SpecifyKind(
    (date ?? DateTime.UtcNow).Date,
    DateTimeKind.Utc
);
        var nextDay = reportDate.AddDays(1);

        var salesInvoices = await _context.SaleInvoices
            .Where(si => si.InvoiceDate >= reportDate &&
                         si.InvoiceDate < nextDay &&
                         si.Status != "Cancelled")
            .Include(si => si.Items)
            .ToListAsync();

        var purchaseInvoices = await _context.PurchaseInvoices
            .Where(pi => pi.InvoiceDate >= reportDate &&
                         pi.InvoiceDate < nextDay &&
                         pi.Status == "Received")
            .ToListAsync();

        var report = BuildDailyReport(reportDate, salesInvoices, purchaseInvoices);

        return Ok(report);
    }
    catch (Exception ex)
    {
        return StatusCode(500, ex.ToString());
    }
}

    // MONTHLY REPORT
    // GET /api/reports/monthly?year=2026&month=4
    // Returns financials for the whole month with a per-day breakdown.

    [HttpGet("monthly")]
    public async Task<IActionResult> Monthly([FromQuery] int? year, [FromQuery] int? month)
    {
        var y = year ?? DateTime.UtcNow.Year;
        var m = month ?? DateTime.UtcNow.Month;

        if (m < 1 || m > 12)
            return BadRequest(new { message = "Month must be between 1 and 12." });

        var start = new DateTime(y, m, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        //  Sales this month 
        var salesInvoices = await _context.SaleInvoices
            .Where(si => si.InvoiceDate >= start && si.InvoiceDate < end && si.Status != "Cancelled")
            .Include(si => si.Items)
            .ToListAsync();

        //  Purchases this month 
        var purchaseInvoices = await _context.PurchaseInvoices
            .Where(pi => pi.InvoiceDate >= start && pi.InvoiceDate < end && pi.Status == "Received")
            .ToListAsync();

        // Per-day sales breakdown 
        var salesByDay = salesInvoices
            .GroupBy(si => si.InvoiceDate.Date)
            .Select(g => new SalesSummaryByDayDto
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                Revenue = g.Sum(si => si.TotalAmount),
                InvoiceCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        //  Top selling parts
        var topParts = GetTopSellingParts(salesInvoices, topN: 10);

        //  Revenue by payment method 
        var byPayment = GetRevenueByPaymentMethod(salesInvoices);

        // Aggregates 
        var totalRevenue = salesInvoices.Sum(si => si.TotalAmount);
        var totalDiscount = salesInvoices.Sum(si => si.DiscountAmount);
        var purchaseCost = purchaseInvoices.Sum(pi => pi.TotalAmount);

        var report = new FinancialReportDto
        {
            Period = $"{y}-{m:D2}",
            ReportType = "Monthly",
            TotalRevenue = totalRevenue,
            TotalDiscount = totalDiscount,
            GrossRevenue = totalRevenue + totalDiscount,
            TotalPurchaseCost = purchaseCost,
            GrossProfit = totalRevenue - purchaseCost,
            TotalSalesInvoices = salesInvoices.Count,
            TotalPurchaseInvoices = purchaseInvoices.Count,
            LoyaltyDiscountCount = salesInvoices.Count(si => si.LoyaltyDiscountApplied),
            SalesByDay = salesByDay,
            TopSellingParts = topParts,
            RevenueByPaymentMethod = byPayment
        };

        return Ok(report);
    }


    // YEARLY REPORT
    // GET /api/reports/yearly?year=2026
    // Returns financials for the whole year with a per-month breakdown.

    [HttpGet("yearly")]
    public async Task<IActionResult> Yearly([FromQuery] int? year)
    {
        var y = year ?? DateTime.UtcNow.Year;
        var start = new DateTime(y, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddYears(1);

        //  Sales this year 
        var salesInvoices = await _context.SaleInvoices
            .Where(si => si.InvoiceDate >= start && si.InvoiceDate < end && si.Status != "Cancelled")
            .Include(si => si.Items)
                .ThenInclude(i => i.Part)
            .ToListAsync();

        //  Purchases this year 
        var purchaseInvoices = await _context.PurchaseInvoices
            .Where(pi => pi.InvoiceDate >= start && pi.InvoiceDate < end && pi.Status == "Received")
            .ToListAsync();


        var monthNames = new[]
        {
            "", "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        };

        var salesByMonth = Enumerable.Range(1, 12).Select(m =>
        {
            var monthSales = salesInvoices.Where(si => si.InvoiceDate.Month == m).ToList();
            var monthPurchases = purchaseInvoices.Where(pi => pi.InvoiceDate.Month == m).ToList();
            var rev = monthSales.Sum(si => si.TotalAmount);
            var cost = monthPurchases.Sum(pi => pi.TotalAmount);

            return new MonthlySummaryDto
            {
                Month = m,
                MonthName = monthNames[m],
                Revenue = rev,
                PurchaseCost = cost,
                Profit = rev - cost,
                InvoiceCount = monthSales.Count
            };
        }).ToList();

        // Top selling parts 
        var topParts = GetTopSellingParts(salesInvoices, topN: 10);

        var byPayment = GetRevenueByPaymentMethod(salesInvoices);

        var totalRevenue = salesInvoices.Sum(si => si.TotalAmount);
        var totalDiscount = salesInvoices.Sum(si => si.DiscountAmount);
        var purchaseCost = purchaseInvoices.Sum(pi => pi.TotalAmount);

        var report = new YearlyFinancialReportDto
        {
            Period = y.ToString(),
            ReportType = "Yearly",
            TotalRevenue = totalRevenue,
            TotalDiscount = totalDiscount,
            GrossRevenue = totalRevenue + totalDiscount,
            TotalPurchaseCost = purchaseCost,
            GrossProfit = totalRevenue - purchaseCost,
            TotalSalesInvoices = salesInvoices.Count,
            TotalPurchaseInvoices = purchaseInvoices.Count,
            LoyaltyDiscountCount = salesInvoices.Count(si => si.LoyaltyDiscountApplied),
            SalesByMonth = salesByMonth,
            TopSellingParts = topParts,
            RevenueByPaymentMethod = byPayment
        };

        return Ok(report);
    }


    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Revenue figures (exclude Cancelled invoices)
        var todayRevenue = await _context.SaleInvoices
            .Where(si => si.InvoiceDate >= todayStart && si.Status != "Cancelled")
            .SumAsync(si => (decimal?)si.TotalAmount) ?? 0;

        var monthRevenue = await _context.SaleInvoices
            .Where(si => si.InvoiceDate >= monthStart && si.Status != "Cancelled")
            .SumAsync(si => (decimal?)si.TotalAmount) ?? 0;

        var yearRevenue = await _context.SaleInvoices
            .Where(si => si.InvoiceDate >= yearStart && si.Status != "Cancelled")
            .SumAsync(si => (decimal?)si.TotalAmount) ?? 0;

        // Counts
        var totalCustomers = await _context.Customers.CountAsync();
        var totalParts = await _context.Parts.CountAsync();
        var lowStockCount = await _context.Parts.CountAsync(p => p.StockQty < 10);
        var pendingCredits = await _context.SaleInvoices.CountAsync(si => si.Status == "Credit");
        var totalVendors = await _context.Vendors.CountAsync();
        var totalStaff = await _context.Staff.CountAsync(s => s.IsActive);

        return Ok(new
        {
            revenue = new
            {
                today = todayRevenue,
                thisMonth = monthRevenue,
                thisYear = yearRevenue
            },
            counts = new
            {
                totalCustomers,
                totalParts,
                totalVendors,
                activeStaff = totalStaff,
                lowStockParts = lowStockCount,
                pendingCreditInvoices = pendingCredits
            }
        });
    }


    private static FinancialReportDto BuildDailyReport(
        DateTime date,
        List<SaleInvoice> sales,
        List<PurchaseInvoice> purchases)
    {
        var totalRevenue = sales.Sum(si => si.TotalAmount);
        var totalDiscount = sales.Sum(si => si.DiscountAmount);
        var purchaseCost = purchases.Sum(pi => pi.TotalAmount);

        return new FinancialReportDto
        {
            Period = date.ToString("yyyy-MM-dd"),
            ReportType = "Daily",
            TotalRevenue = totalRevenue,
            TotalDiscount = totalDiscount,
            GrossRevenue = totalRevenue + totalDiscount,
            TotalPurchaseCost = purchaseCost,
            GrossProfit = totalRevenue - purchaseCost,
            TotalSalesInvoices = sales.Count,
            TotalPurchaseInvoices = purchases.Count,
            LoyaltyDiscountCount = sales.Count(si => si.LoyaltyDiscountApplied),
            SalesByDay = new List<SalesSummaryByDayDto>(), // not needed for daily
            TopSellingParts = GetTopSellingParts(sales, topN: 5),
            RevenueByPaymentMethod = GetRevenueByPaymentMethod(sales)
        };
    }


    private static List<TopSellingPartDto> GetTopSellingParts(List<SaleInvoice> invoices, int topN)
{
    return invoices
        .Where(si => si.Items != null)
        .SelectMany(si => si.Items)
        .Where(i => i.Part != null)
        .GroupBy(i => new
        {
            i.PartId,
            Name = i.Part.Name,
            PartNumber = i.Part.PartNumber
        })
        .Select(g => new TopSellingPartDto
        {
            PartId = g.Key.PartId,
            PartName = g.Key.Name,
            PartNumber = g.Key.PartNumber,
            QuantitySold = g.Sum(i => i.Quantity),
            TotalRevenue = g.Sum(i => i.Quantity * i.UnitPrice)
        })
        .OrderByDescending(x => x.QuantitySold)
        .Take(topN)
        .ToList();
}


    private static List<PaymentMethodSummaryDto> GetRevenueByPaymentMethod(List<SaleInvoice> invoices)
{
    return invoices
        .GroupBy(si => si.PaymentMethod ?? "Unknown")
        .Select(g => new PaymentMethodSummaryDto
        {
            PaymentMethod = g.Key,
            InvoiceCount = g.Count(),
            TotalRevenue = g.Sum(si => si.TotalAmount)
        })
        .OrderByDescending(x => x.TotalRevenue)
        .ToList();
}
}