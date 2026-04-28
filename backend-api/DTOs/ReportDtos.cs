namespace VehiclePartsAPI.DTOs;

// ── Financial Report Response DTOs ───────────────────────────

/// <summary>Top-level financial report returned for daily/monthly/yearly queries.</summary>
public class FinancialReportDto
{
    public string Period { get; set; } = string.Empty;          // e.g. "2026-04-27" | "2026-04" | "2026"
    public string ReportType { get; set; } = string.Empty;      // Daily | Monthly | Yearly

    // Revenue
    public decimal TotalRevenue { get; set; }                   // sum of sale invoice totals
    public decimal TotalDiscount { get; set; }                  // sum of discounts given
    public decimal GrossRevenue { get; set; }                   // TotalRevenue + TotalDiscount (before discount)

    // Cost
    public decimal TotalPurchaseCost { get; set; }              // sum of purchase invoice totals

    // Profit
    public decimal GrossProfit { get; set; }                    // TotalRevenue - TotalPurchaseCost

    // Counts
    public int TotalSalesInvoices { get; set; }
    public int TotalPurchaseInvoices { get; set; }
    public int LoyaltyDiscountCount { get; set; }               // invoices where loyalty was applied

    // Breakdowns
    public List<SalesSummaryByDayDto> SalesByDay { get; set; } = new();     // for monthly/yearly
    public List<TopSellingPartDto> TopSellingParts { get; set; } = new();
    public List<PaymentMethodSummaryDto> RevenueByPaymentMethod { get; set; } = new();
}

/// <summary>Daily revenue row used inside monthly and yearly reports.</summary>
public class SalesSummaryByDayDto
{
    public string Date { get; set; } = string.Empty;            // "2026-04-27"
    public decimal Revenue { get; set; }
    public int InvoiceCount { get; set; }
}

/// <summary>Top selling parts by quantity sold in the period.</summary>
public class TopSellingPartDto
{
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}

/// <summary>Revenue grouped by payment method (Cash, Credit, Card).</summary>
public class PaymentMethodSummaryDto
{
    public string PaymentMethod { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal TotalRevenue { get; set; }
}

/// <summary>Compact month row used inside yearly reports.</summary>
public class MonthlySummaryDto
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal Profit { get; set; }
    public int InvoiceCount { get; set; }
}

/// <summary>Full yearly report (replaces SalesByDay with SalesByMonth).</summary>
public class YearlyFinancialReportDto
{
    public string Period { get; set; } = string.Empty;
    public string ReportType { get; set; } = "Yearly";

    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal TotalPurchaseCost { get; set; }
    public decimal GrossProfit { get; set; }

    public int TotalSalesInvoices { get; set; }
    public int TotalPurchaseInvoices { get; set; }
    public int LoyaltyDiscountCount { get; set; }

    public List<MonthlySummaryDto> SalesByMonth { get; set; } = new();
    public List<TopSellingPartDto> TopSellingParts { get; set; } = new();
    public List<PaymentMethodSummaryDto> RevenueByPaymentMethod { get; set; } = new();
}
