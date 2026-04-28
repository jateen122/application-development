namespace VehiclePartsAPI.Models;

public class SaleInvoiceItem
{
    public int Id { get; set; }

    public int PartId { get; set; }
    public Part Part { get; set; }

    public int Quantity { get; set; }
    public decimal Price { get; set; }

    public int SaleInvoiceId { get; set; }
    public SaleInvoice SaleInvoice { get; set; }
}