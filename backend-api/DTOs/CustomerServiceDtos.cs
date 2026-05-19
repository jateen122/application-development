namespace VehiclePartsAPI.DTOs;

// Appointments 

public class CreateAppointmentDto
{
    public int      CustomerId      { get; set; }
    public string   ServiceType     { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string   Notes           { get; set; } = string.Empty;
}

public class AppointmentDto
{
    public int      Id              { get; set; }
    public int      CustomerId      { get; set; }
    public string   CustomerName    { get; set; } = string.Empty;
    public string   CustomerPhone   { get; set; } = string.Empty;
    public string   ServiceType     { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public string   Notes           { get; set; } = string.Empty;
    public string   Status          { get; set; } = string.Empty;
    public DateTime CreatedAt       { get; set; }
}

// Part Requests 

public class CreatePartRequestDto
{
    public int    CustomerId { get; set; }
    public string PartName   { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public int    Quantity   { get; set; } = 1;
    public string Notes      { get; set; } = string.Empty;
}

public class PartRequestDto
{
    public int      Id           { get; set; }
    public int      CustomerId   { get; set; }
    public string   CustomerName { get; set; } = string.Empty;
    public string   PartName     { get; set; } = string.Empty;
    public string   PartNumber   { get; set; } = string.Empty;
    public int      Quantity     { get; set; }
    public string   Notes        { get; set; } = string.Empty;
    public string   Status       { get; set; } = string.Empty;
    public DateTime CreatedAt    { get; set; }
}

public class CreateReviewDto
{
    public int    CustomerId { get; set; }
    public int    Rating     { get; set; }  
    public string Comment    { get; set; } = string.Empty;
}

public class ReviewDto
{
    public int      Id           { get; set; }
    public int      CustomerId   { get; set; }
    public string   CustomerName { get; set; } = string.Empty;
    public int      Rating       { get; set; }
    public string   Comment      { get; set; } = string.Empty;
    public DateTime CreatedAt    { get; set; }
}


public class UpdateStatusDto
{
    public string Status { get; set; } = string.Empty;
}


public class UpdateSaleStatusDto
{
    /// <summary>Allowed: Paid | Cancelled</summary>
    public string Status { get; set; } = string.Empty;
}



public class ChangePasswordDto
{
    public string Password { get; set; } = string.Empty;
}
