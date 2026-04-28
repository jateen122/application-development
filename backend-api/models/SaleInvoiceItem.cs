namespace VehiclePartsAPI.Models;

public class SaleInvoiceItem
{
    public int Id { get; set; }

    public int PartId { get; set; }
    public Part Part { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }   // selling price at time of sale

    public int SaleInvoiceId { get; set; }
    public SaleInvoice SaleInvoice { get; set; } = null!;
}
