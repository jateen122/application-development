namespace VehiclePartsAPI.Models;

public class SaleInvoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; }
    public DateTime Date { get; set; }
    public decimal TotalAmount { get; set; }

    public int CustomerId { get; set; }

    public List<SaleInvoiceItem> Items { get; set; } = new();
}