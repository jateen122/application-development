namespace VehiclePartsAPI.Models;

public class PurchaseInvoiceItem
{
    public int Id { get; set; }

    public int PartId { get; set; }
    public Part Part { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }   // cost paid to vendor

    public int PurchaseInvoiceId { get; set; }
    public PurchaseInvoice PurchaseInvoice { get; set; } = null!;
}
