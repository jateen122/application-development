namespace VehiclePartsAPI.Models;

public class Vehicle
{
    public int Id { get; set; }

    public string VehicleNumber { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Color { get; set; } = string.Empty;
    public string FuelType { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
