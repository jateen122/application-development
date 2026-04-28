namespace VehiclePartsAPI.Models;

public class Part
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }

    public int StockQty { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation — needed by AppDbContext and ReportsController
    public List<SaleInvoiceItem> SaleInvoiceItems { get; set; } = new();
    public List<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; } = new();
}
