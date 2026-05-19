using Microsoft.EntityFrameworkCore;
using VehiclePartsAPI.Models;

namespace VehiclePartsAPI.Data
{
    /// <summary>
    /// Entity Framework Core database context for the Vehicle Parts System.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ── DbSets ────────────────────────────────────────────────
        public DbSet<Vendor>              Vendors              { get; set; }
        public DbSet<Part>                Parts                { get; set; }
        public DbSet<PurchaseInvoice>     PurchaseInvoices     { get; set; }
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems { get; set; }
        public DbSet<Customer>            Customers            { get; set; }
        public DbSet<Vehicle>             Vehicles             { get; set; }
        public DbSet<SaleInvoice>         SaleInvoices         { get; set; }
        public DbSet<SaleInvoiceItem>     SaleInvoiceItems     { get; set; }
        public DbSet<Staff>               Staff                { get; set; }

        // ── Feature 13 ────────────────────────────────────────────
        public DbSet<Appointment>   Appointments   { get; set; }
        public DbSet<PartRequest>   PartRequests   { get; set; }
        public DbSet<ServiceReview> ServiceReviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ── Vendor ──────────────────────────────────────────────
            modelBuilder.Entity<Vendor>(b =>
            {
                b.HasIndex(v => v.Email).IsUnique();
                b.Property(v => v.CreatedAt).HasDefaultValueSql("NOW()");
            });

            // ── Part ────────────────────────────────────────────────
            modelBuilder.Entity<Part>(b =>
            {
                b.HasIndex(p => p.PartNumber).IsUnique();
                b.Property(p => p.CostPrice).HasColumnType("numeric(18,2)");
                b.Property(p => p.SellingPrice).HasColumnType("numeric(18,2)");
            });

            // ── PurchaseInvoice → Vendor ─────────────────────────────
            modelBuilder.Entity<PurchaseInvoice>(b =>
            {
                b.HasOne(pi => pi.Vendor)
                 .WithMany(v => v.PurchaseInvoices)
                 .HasForeignKey(pi => pi.VendorId)
                 .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(pi => pi.InvoiceNumber).IsUnique();
                b.Property(pi => pi.TotalAmount).HasColumnType("numeric(18,2)");
            });

            // ── PurchaseInvoiceItem ──────────────────────────────────
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

            // ── Customer ─────────────────────────────────────────────
            modelBuilder.Entity<Customer>(b =>
            {
                b.HasIndex(c => c.Email).IsUnique();
                b.Property(c => c.CreatedAt).HasDefaultValueSql("NOW()");
            });

            // ── Vehicle → Customer ───────────────────────────────────
            modelBuilder.Entity<Vehicle>(b =>
            {
                b.HasIndex(v => v.VehicleNumber).IsUnique();

                b.HasOne(v => v.Customer)
                 .WithMany(c => c.Vehicles)
                 .HasForeignKey(v => v.CustomerId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ── SaleInvoice → Customer ───────────────────────────────
            modelBuilder.Entity<SaleInvoice>(b =>
            {
                b.HasOne(si => si.Customer)
                 .WithMany(c => c.SaleInvoices)
                 .HasForeignKey(si => si.CustomerId)
                 .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(si => si.InvoiceNumber).IsUnique();
                b.Property(si => si.SubTotal).HasColumnType("numeric(18,2)");
                b.Property(si => si.DiscountAmount).HasColumnType("numeric(18,2)");
                b.Property(si => si.TotalAmount).HasColumnType("numeric(18,2)");
            });

            // ── SaleInvoiceItem ──────────────────────────────────────
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

            // ── Staff ────────────────────────────────────────────────
            modelBuilder.Entity<Staff>(b =>
            {
                b.HasIndex(s => s.Email).IsUnique();
                b.Property(s => s.CreatedAt).HasDefaultValueSql("NOW()");
            });

            // ── Appointment → Customer ───────────────────────────────
            modelBuilder.Entity<Appointment>(b =>
            {
                b.HasOne(a => a.Customer)
                 .WithMany()
                 .HasForeignKey(a => a.CustomerId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.Property(a => a.CreatedAt).HasDefaultValueSql("NOW()");
            });

            // ── PartRequest → Customer ───────────────────────────────
            modelBuilder.Entity<PartRequest>(b =>
            {
                b.HasOne(r => r.Customer)
                 .WithMany()
                 .HasForeignKey(r => r.CustomerId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.Property(r => r.CreatedAt).HasDefaultValueSql("NOW()");
            });

            // ── ServiceReview → Customer ─────────────────────────────
            modelBuilder.Entity<ServiceReview>(b =>
            {
                b.HasOne(r => r.Customer)
                 .WithMany()
                 .HasForeignKey(r => r.CustomerId)
                 .OnDelete(DeleteBehavior.Cascade);

                b.Property(r => r.CreatedAt).HasDefaultValueSql("NOW()");
            });
        }
    }
}
