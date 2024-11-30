
using InvestmentManager.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InvestmentManager.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public required DbSet<Transaction> Transactions { get; set; }
    public required DbSet<Transaction> Assets { get; set; }
}
