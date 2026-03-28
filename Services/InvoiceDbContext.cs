using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using ShInvoicing.Models;

namespace ShInvoicing.Services;

public class InvoiceDbContext : DbContext
{
    public DbSet<Invoice> Invoices { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "invoices.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Invoice>()
            .HasKey(i => i.InvoiceNo);

        modelBuilder.Entity<Invoice>()
            .OwnsMany(i => i.Items, item =>
            {
                item.WithOwner().HasForeignKey("InvoiceNo");
                item.HasKey("InvoiceNo", "Description"); // Composite key
            });
    }
}