namespace VehiclePartsAPI.DTOs;

public class CreateSaleInvoiceDto
{
    public int CustomerId { get; set; }
    public string PaymentMethod { get; set; }

    public List<CreateSaleItemDto> Items { get; set; } = new();
}

public class CreateSaleItemDto
{
    public int PartId { get; set; }
    public int Quantity { get; set; }
}

public class SaleInvoiceDetailDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; }
    public DateTime Date { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; }

    public List<SaleItemDto> Items { get; set; } = new();
}

public class SaleItemDto
{
    public int PartId { get; set; }
    public string PartName { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}