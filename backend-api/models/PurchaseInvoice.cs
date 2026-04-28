namespace VehiclePartsAPI.Models
{
    public class PurchaseInvoice
    {
        public int Id { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;  // PI-2026-0001
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Received";  // Pending | Received | Cancelled

        public int VendorId { get; set; }
        public Vendor Vendor { get; set; } = null!;

        public List<PurchaseInvoiceItem> Items { get; set; } = new();
    }
}
