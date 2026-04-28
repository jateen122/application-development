namespace VehiclePartsAPI.Models;

public class Part
{
    public int Id { get; set; }

    public string Name { get; set; }
    public string PartNumber { get; set; }

    public string Description { get; set; }
    public string Category { get; set; }

    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }

    public int StockQty { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}