namespace VehiclePartsAPI.DTOs
{
    // Request DTOs 

    public class CreatePurchaseInvoiceDto
    {
        public int VendorId { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public List<CreatePurchaseItemDto> Items { get; set; } = new();
    }

    public class CreatePurchaseItemDto
    {
        public int PartId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }

    public class UpdatePurchaseInvoiceStatusDto
    {
        public string Status { get; set; } = string.Empty;  // Pending | Received | Cancelled
    }

    // Response DTOs

    /// <summary>Summary row used in list endpoints.</summary>
    public class PurchaseInvoiceDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public int VendorId { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
    }

    public class PurchaseInvoiceDetailDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public VehiclePartsAPI.DTOs.VendorDto Vendor { get; set; } = null!;
        public List<PurchaseInvoiceItemDto> Items { get; set; } = new();
    }

    public class PurchaseInvoiceItemDto
    {
        public int PartId { get; set; }
        public string PartName { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal LineTotal { get; set; }
    }
}
