namespace VehiclePartsAPI.DTOs;

// Request 

public class LoginDto
{
    /// <summary>Email address of the user.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Plain-text password (compared against BCrypt hash).</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Declared role: Admin | Staff | Customer</summary>
    public string Role { get; set; } = string.Empty;
}


public class LoginResponseDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
