namespace VehiclePartsAPI.DTOs;

public class FinancialReportDto
{
    public string Period { get; set; } = string.Empty;          
    public string ReportType { get; set; } = string.Empty;      

    // Revenue
    public decimal TotalRevenue { get; set; }                   
    public decimal TotalDiscount { get; set; }                
    public decimal GrossRevenue { get; set; }                   

    // Cost
    public decimal TotalPurchaseCost { get; set; }              

    // Profit
    public decimal GrossProfit { get; set; }                    

    // Counts
    public int TotalSalesInvoices { get; set; }
    public int TotalPurchaseInvoices { get; set; }
    public int LoyaltyDiscountCount { get; set; }               

    // Breakdowns
    public List<SalesSummaryByDayDto> SalesByDay { get; set; } = new();    
    public List<TopSellingPartDto> TopSellingParts { get; set; } = new();
    public List<PaymentMethodSummaryDto> RevenueByPaymentMethod { get; set; } = new();
}

public class SalesSummaryByDayDto
{
    public string Date { get; set; } = string.Empty;            
    public decimal Revenue { get; set; }
    public int InvoiceCount { get; set; }
}

public class TopSellingPartDto
{
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class PaymentMethodSummaryDto
{
    public string PaymentMethod { get; set; } = string.Empty;
    public int InvoiceCount { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class MonthlySummaryDto
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal PurchaseCost { get; set; }
    public decimal Profit { get; set; }
    public int InvoiceCount { get; set; }
}

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
