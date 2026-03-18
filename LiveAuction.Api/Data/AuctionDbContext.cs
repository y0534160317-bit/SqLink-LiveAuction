using Microsoft.EntityFrameworkCore;
using LiveAuction.Api.Models;

namespace LiveAuction.Api.Data;

public class AuctionDbContext : DbContext
{
    public AuctionDbContext(DbContextOptions<AuctionDbContext> options) : base(options) { }

    // הגדרת הטבלאות בבסיס הנתונים
    public DbSet<Auction> Auctions { get; set; }
    public DbSet<Bid> Bids { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. התאמה ל-SQLite: המרת decimal ל-double (SQLite לא תומך ב-decimal בחישובים)
        modelBuilder.Entity<Auction>()
            .Property(a => a.StartingPrice)
            .HasConversion<double>();

        modelBuilder.Entity<Auction>()
            .Property(a => a.CurrentHighPrice)
            .HasConversion<double>();

        modelBuilder.Entity<Bid>()
            .Property(b => b.Amount)
            .HasConversion<double>();

        // 2. הגדרת ניהול מקביליות (Concurrency)
        // הגדרנו את RowVersion במודל כ-string, כאן אנחנו אומרים ל-EF לעקוב אחריו
        modelBuilder.Entity<Auction>()
            .Property(a => a.RowVersion)
            .IsConcurrencyToken();

        // 3. הגדרת קשרים בין הטבלאות (Fluent API)
        
        // קשר בין מכירה להצעות (1-to-Many)
        modelBuilder.Entity<Bid>()
            .HasOne(b => b.Auction)
            .WithMany(a => a.Bids)
            .HasForeignKey(b => b.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        // קשר בין משתמש להצעות (1-to-Many)
        modelBuilder.Entity<Bid>()
            .HasOne(b => b.User)
            .WithMany(u => u.Bids)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}