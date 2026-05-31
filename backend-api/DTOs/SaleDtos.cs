namespace VehiclePartsAPI.DTOs;

//Request DTOs 

public class CreateSaleInvoiceDto
{
    public int CustomerId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;   // Cash | Card | Credit
    public List<CreateSaleItemDto> Items { get; set; } = new();
}

public class CreateSaleItemDto
{
    public int PartId { get; set; }
    public int Quantity { get; set; }
}

// Response DTOs 

public class SaleInvoiceSummaryDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool LoyaltyDiscountApplied { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
}

public class SaleInvoiceDetailDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool LoyaltyDiscountApplied { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public CustomerDto Customer { get; set; } = null!;
    public List<SaleInvoiceItemDto> Items { get; set; } = new();
}

public class SaleInvoiceItemDto
{
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
