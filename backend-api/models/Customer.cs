namespace VehiclePartsAPI.Models;

public class Customer
{
    public int Id { get; set; }

    public string FirstName { get; set; }
    public string LastName { get; set; }

    public string Email { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string PasswordHash { get; set; }
    public string Role { get; set; }

    public List<Vehicle> Vehicles { get; set; } = new();
    public List<SaleInvoice> SaleInvoices { get; set; } = new();
}