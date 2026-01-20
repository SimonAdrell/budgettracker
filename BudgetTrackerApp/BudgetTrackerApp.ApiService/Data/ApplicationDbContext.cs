using BudgetTrackerApp.ApiService.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BudgetTrackerApp.ApiService.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<AccountUser> AccountUsers { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<BalanceSnapshot> BalanceSnapshots { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure RefreshToken relationship
        builder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Create index on Token for faster lookups
        builder.Entity<RefreshToken>()
            .HasIndex(rt => rt.Token)
            .IsUnique();

        // Configure Account entity
        builder.Entity<Account>()
            .Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Entity<Account>()
            .Property(a => a.AccountNumber)
            .HasMaxLength(50);

        // Configure AccountUser (many-to-many junction table)
        builder.Entity<AccountUser>()
            .HasOne(au => au.User)
            .WithMany(u => u.AccountUsers)
            .HasForeignKey(au => au.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AccountUser>()
            .HasOne(au => au.Account)
            .WithMany(a => a.AccountUsers)
            .HasForeignKey(au => au.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AccountUser>()
            .Property(au => au.Role)
            .IsRequired()
            .HasMaxLength(50);

        // Create unique index to prevent duplicate user-account pairs
        builder.Entity<AccountUser>()
            .HasIndex(au => new { au.UserId, au.AccountId })
            .IsUnique();

        // Configure Category entity
        builder.Entity<Category>()
            .Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Entity<Category>()
            .Property(c => c.Description)
            .HasMaxLength(500);

        builder.Entity<Category>()
            .Property(c => c.Color)
            .HasMaxLength(20);

        // Configure self-referencing relationship for hierarchical categories
        builder.Entity<Category>()
            .HasOne(c => c.ParentCategory)
            .WithMany(c => c.ChildCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Transaction entity
        builder.Entity<Transaction>()
            .HasOne(t => t.Account)
            .WithMany(a => a.Transactions)
            .HasForeignKey(t => t.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Transaction>()
            .HasOne(t => t.Category)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<Transaction>()
            .Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Entity<Transaction>()
            .Property(t => t.Amount)
            .HasPrecision(18, 2);

        builder.Entity<Transaction>()
            .Property(t => t.Balance)
            .HasPrecision(18, 2);

        // Create index on TransactionDate for faster date-based queries
        builder.Entity<Transaction>()
            .HasIndex(t => t.TransactionDate);

        // Configure BalanceSnapshot entity
        builder.Entity<BalanceSnapshot>()
            .HasOne(bs => bs.Account)
            .WithMany(a => a.BalanceSnapshots)
            .HasForeignKey(bs => bs.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BalanceSnapshot>()
            .Property(bs => bs.Balance)
            .HasPrecision(18, 2);

        // Create unique index to prevent duplicate snapshots for the same date
        builder.Entity<BalanceSnapshot>()
            .HasIndex(bs => new { bs.AccountId, bs.SnapshotDate })
            .IsUnique();
    }
}

