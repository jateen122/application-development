using Microsoft.EntityFrameworkCore;
using VehiclePartsAPI.Models;
/// <summary>
/// Entity Framework Core database context for the Vehicle Parts System.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── DbSets ────────────────────────────────────────────────
    public DbSet<Vendor> Vendors { get; set; }
    public DbSet<Part> Parts { get; set; }
    public DbSet<PurchaseInvoice> PurchaseInvoices { get; set; }
    public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Vehicle> Vehicles { get; set; }
    public DbSet<SaleInvoice> SaleInvoices { get; set; }
    public DbSet<SaleInvoiceItem> SaleInvoiceItems { get; set; }
    public DbSet<Staff> Staff { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Vendor ──────────────────────────────────────────────
        modelBuilder.Entity<Vendor>(b =>
        {
            b.HasIndex(v => v.Email).IsUnique();
        });

        // ── Part ────────────────────────────────────────────────
        modelBuilder.Entity<Part>(b =>
        {
            b.HasIndex(p => p.PartNumber).IsUnique();
            b.Property(p => p.CostPrice).HasColumnType("numeric(18,2)");
            b.Property(p => p.SellingPrice).HasColumnType("numeric(18,2)");
        });

        // ── PurchaseInvoice → Vendor (M-to-1) ───────────────────
        modelBuilder.Entity<PurchaseInvoice>(b =>
        {
            b.HasOne(pi => pi.Vendor)
             .WithMany(v => v.PurchaseInvoices)
             .HasForeignKey(pi => pi.VendorId)
             .OnDelete(DeleteBehavior.Restrict);

            b.Property(pi => pi.TotalAmount).HasColumnType("numeric(18,2)");
        });

        // ── PurchaseInvoiceItem → PurchaseInvoice and Part ──────
        modelBuilder.Entity<PurchaseInvoiceItem>(b =>
        {
            b.HasOne(i => i.PurchaseInvoice)
             .WithMany(pi => pi.Items)
             .HasForeignKey(i => i.PurchaseInvoiceId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(i => i.Part)
             .WithMany(p => p.PurchaseInvoiceItems)
             .HasForeignKey(i => i.PartId)
             .OnDelete(DeleteBehavior.Restrict);

            b.Property(i => i.UnitCost).HasColumnType("numeric(18,2)");
        });

        // ── Customer ────────────────────────────────────────────
        modelBuilder.Entity<Customer>(b =>
        {
            b.HasIndex(c => c.Email).IsUnique();
        });

        // ── Vehicle → Customer (M-to-1) ─────────────────────────
        modelBuilder.Entity<Vehicle>(b =>
        {
            b.HasIndex(v => v.VehicleNumber).IsUnique();

            b.HasOne(v => v.Customer)
             .WithMany(c => c.Vehicles)
             .HasForeignKey(v => v.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── SaleInvoice → Customer (M-to-1) ─────────────────────
        modelBuilder.Entity<SaleInvoice>(b =>
        {
            b.HasOne(si => si.Customer)
             .WithMany(c => c.SaleInvoices)
             .HasForeignKey(si => si.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);

            b.Property(si => si.SubTotal).HasColumnType("numeric(18,2)");
            b.Property(si => si.DiscountAmount).HasColumnType("numeric(18,2)");
            b.Property(si => si.TotalAmount).HasColumnType("numeric(18,2)");
        });

        // ── SaleInvoiceItem → SaleInvoice and Part ──────────────
        modelBuilder.Entity<SaleInvoiceItem>(b =>
        {
            b.HasOne(i => i.SaleInvoice)
             .WithMany(si => si.Items)
             .HasForeignKey(i => i.SaleInvoiceId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(i => i.Part)
             .WithMany(p => p.SaleInvoiceItems)
             .HasForeignKey(i => i.PartId)
             .OnDelete(DeleteBehavior.Restrict);

            b.Property(i => i.UnitPrice).HasColumnType("numeric(18,2)");
        });

        // ── Staff ───────────────────────────────────────────────
        modelBuilder.Entity<Staff>(b =>
        {
            b.HasIndex(s => s.Email).IsUnique();
        });
    }
}
