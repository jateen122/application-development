using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehiclePartsAPI.Models;
using VehiclePartsAPI.DTOs;
using VehiclePartsAPI.Data;

namespace VehiclePartsAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PartsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PartsController(AppDbContext context) => _context = context;


        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? category, [FromQuery] string? search)
        {
            var query = _context.Parts.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category == category);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.Contains(search) || p.PartNumber.Contains(search));

            var parts = await query
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .Select(p => new PartDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    PartNumber = p.PartNumber,
                    Description = p.Description,
                    Category = p.Category,
                    CostPrice = p.CostPrice,
                    SellingPrice = p.SellingPrice,
                    StockQty = p.StockQty,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return Ok(parts);
        }

        // GET /api/parts/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part == null)
                return NotFound(new { message = $"Part with ID {id} not found." });

            return Ok(MapToDto(part));
        }

        // POST /api/parts
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePartDto dto)
        {
            var exists = await _context.Parts.AnyAsync(p => p.PartNumber == dto.PartNumber);
            if (exists)
                return Conflict(new { message = $"A part with part number '{dto.PartNumber}' already exists." });

            if (dto.SellingPrice < dto.CostPrice)
                return BadRequest(new { message = "Selling price should not be less than cost price." });

            var part = new Part
            {
                Name = dto.Name,
                PartNumber = dto.PartNumber,
                Description = dto.Description,
                Category = dto.Category,
                CostPrice = dto.CostPrice,
                SellingPrice = dto.SellingPrice,
                StockQty = dto.StockQty
            };

            _context.Parts.Add(part);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = part.Id }, MapToDto(part));
        }

        // PUT /api/parts/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePartDto dto)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part == null)
                return NotFound(new { message = $"Part with ID {id} not found." });

            var conflict = await _context.Parts.AnyAsync(p => p.PartNumber == dto.PartNumber && p.Id != id);
            if (conflict)
                return Conflict(new { message = $"Another part already uses part number '{dto.PartNumber}'." });

            part.Name = dto.Name;
            part.PartNumber = dto.PartNumber;
            part.Description = dto.Description;
            part.Category = dto.Category;
            part.CostPrice = dto.CostPrice;
            part.SellingPrice = dto.SellingPrice;
            part.StockQty = dto.StockQty;
            part.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/parts/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part == null)
                return NotFound(new { message = $"Part with ID {id} not found." });

            var usedInSales = await _context.SaleInvoiceItems.AnyAsync(si => si.PartId == id);
            var usedInPurchase = await _context.PurchaseInvoiceItems.AnyAsync(pi => pi.PartId == id);

            if (usedInSales || usedInPurchase)
                return BadRequest(new { message = "Cannot delete part — it is referenced in existing invoices." });

            _context.Parts.Remove(part);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET /api/parts/low-stock
        [HttpGet("low-stock")]
        public async Task<IActionResult> LowStock()
        {
            var parts = await _context.Parts
                .Where(p => p.StockQty < 10)
                .OrderBy(p => p.StockQty)
                .Select(p => new PartDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    PartNumber = p.PartNumber,
                    Description = p.Description,
                    Category = p.Category,
                    CostPrice = p.CostPrice,
                    SellingPrice = p.SellingPrice,
                    StockQty = p.StockQty,
                    UpdatedAt = p.UpdatedAt
                })
                .ToListAsync();

            return Ok(new { totalLowStockParts = parts.Count, parts });
        }

        // GET /api/parts/categories
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Parts
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Ok(categories);
        }

        // GET /api/parts/count
        [HttpGet("count")]
        public async Task<IActionResult> Count()
            => Ok(new { totalParts = await _context.Parts.CountAsync() });

        // Helpers
        private static PartDto MapToDto(Part p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            PartNumber = p.PartNumber,
            Description = p.Description,
            Category = p.Category,
            CostPrice = p.CostPrice,
            SellingPrice = p.SellingPrice,
            StockQty = p.StockQty,
            UpdatedAt = p.UpdatedAt
        };
    }
}
