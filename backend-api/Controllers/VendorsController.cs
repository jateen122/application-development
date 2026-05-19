using VehiclePartsAPI.Models;
using VehiclePartsAPI.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
/// <summary>
/// Feature 5 — Admin can manage vendor details (CRUD operations).
/// Endpoints: GET /api/vendors, GET /api/vendors/{id},
///            POST /api/vendors, PUT /api/vendors/{id}, DELETE /api/vendors/{id}
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VendorsController : ControllerBase
{
    private readonly AppDbContext _context;

    public VendorsController(AppDbContext context) => _context = context;

    // GET /api/vendors
    // Returns all vendors.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var vendors = await _context.Vendors
            .OrderBy(v => v.Name)
            .Select(v => new VendorDto
            {
                Id = v.Id,
                Name = v.Name,
                ContactPerson = v.ContactPerson,
                Email = v.Email,
                Phone = v.Phone,
                Address = v.Address,
                CreatedAt = v.CreatedAt
            })
            .ToListAsync();

        return Ok(vendors);
    }

    // GET /api/vendors/{id}
    // Returns a single vendor by ID.
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var vendor = await _context.Vendors.FindAsync(id);
        if (vendor == null)
            return NotFound(new { message = $"Vendor with ID {id} not found." });

        return Ok(MapToDto(vendor));
    }

    // POST /api/vendors
    // Creates a new vendor.
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVendorDto dto)
    {

        var emailExists = await _context.Vendors.AnyAsync(v => v.Email == dto.Email);
        if (emailExists)
            return Conflict(new { message = "A vendor with this email already exists." });

        var vendor = new Vendor
        {
            Name = dto.Name,
            ContactPerson = dto.ContactPerson,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address
        };

        _context.Vendors.Add(vendor);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = vendor.Id }, MapToDto(vendor));
    }

    // PUT /api/vendors/{id}
    // Updates an existing vendor.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVendorDto dto)
    {
        var vendor = await _context.Vendors.FindAsync(id);
        if (vendor == null)
            return NotFound(new { message = $"Vendor with ID {id} not found." });

        // Check that the new email doesn't belong to another vendor
        var emailConflict = await _context.Vendors.AnyAsync(v => v.Email == dto.Email && v.Id != id);
        if (emailConflict)
            return Conflict(new { message = "Another vendor is already using this email." });

        vendor.Name = dto.Name;
        vendor.ContactPerson = dto.ContactPerson;
        vendor.Email = dto.Email;
        vendor.Phone = dto.Phone;
        vendor.Address = dto.Address;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /api/vendors/{id}
    // Deletes a vendor (only if no associated purchase invoices exist).
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var vendor = await _context.Vendors.FindAsync(id);
        if (vendor == null)
            return NotFound(new { message = $"Vendor with ID {id} not found." });

        // Guard: cannot delete if invoices are linked
        var hasInvoices = await _context.PurchaseInvoices.AnyAsync(pi => pi.VendorId == id);
        if (hasInvoices)
            return BadRequest(new { message = "Cannot delete vendor — existing purchase invoices are linked to this vendor." });

        _context.Vendors.Remove(vendor);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // GET /api/vendors/{id}/invoices
    // Returns all purchase invoices for a vendor (summary).
    [HttpGet("{id:int}/invoices")]
    public async Task<IActionResult> GetInvoices(int id)
    {
        var exists = await _context.Vendors.AnyAsync(v => v.Id == id);
        if (!exists)
            return NotFound(new { message = $"Vendor with ID {id} not found." });

        var invoices = await _context.PurchaseInvoices
            .Where(pi => pi.VendorId == id)
            .OrderByDescending(pi => pi.InvoiceDate)
            .Select(pi => new PurchaseInvoiceDto
            {
                Id = pi.Id,
                InvoiceNumber = pi.InvoiceNumber,
                InvoiceDate = pi.InvoiceDate,
                TotalAmount = pi.TotalAmount,
                Status = pi.Status,
                VendorId = pi.VendorId,
                VendorName = pi.Vendor.Name,
                ItemCount = pi.Items.Count
            })
            .ToListAsync();

        return Ok(invoices);
    }

    private static VendorDto MapToDto(Vendor v) => new()
    {
        Id = v.Id,
        Name = v.Name,
        ContactPerson = v.ContactPerson,
        Email = v.Email,
        Phone = v.Phone,
        Address = v.Address,
        CreatedAt = v.CreatedAt
    };
}
