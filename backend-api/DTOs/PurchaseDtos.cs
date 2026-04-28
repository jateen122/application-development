namespace VehiclePartsAPI.DTOs
{
    public class CreatePurchaseInvoiceDto
    {
        public int VendorId { get; set; }

        public List<CreatePurchaseItemDto> Items { get; set; }
    }

    public class CreatePurchaseItemDto
    {
        public int PartId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class UpdatePurchaseInvoiceStatusDto
    {
        public string Status { get; set; }
    }

    public class PurchaseInvoiceDetailDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }

        public List<PurchaseItemDto> Items { get; set; }
    }

    public class PurchaseItemDto
    {
        public int PartId { get; set; }
        public string PartName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}