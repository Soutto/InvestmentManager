
using InvestmentManager.Shared.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InvestmentManager.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public required DbSet<Transaction> Transactions { get; set; }
    public required DbSet<Asset> Assets { get; set; }
    public required DbSet<AssetMonthlyPrice> AssetMonthlyPrices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Call the base method to ensure Identity entities are configured
        base.OnModelCreating(modelBuilder);

        // Now add your custom configuration
        modelBuilder.Entity<AssetMonthlyPrice>(entity =>
        {
            entity
                .HasKey(e => new { e.AssetIsinCode, e.Year, e.Month });

            entity
                .HasIndex(p => new { p.AssetIsinCode, p.Year, p.Month })
                .HasDatabaseName("AssetIsinCode_Year_Month_Index")
                .IncludeProperties(p => p.Price);
        });
    }
}
