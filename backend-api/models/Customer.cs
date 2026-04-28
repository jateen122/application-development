namespace VehiclePartsAPI.Models;

public class Customer
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer";

    public List<Vehicle> Vehicles { get; set; } = new();
    public List<SaleInvoice> SaleInvoices { get; set; } = new();
}
