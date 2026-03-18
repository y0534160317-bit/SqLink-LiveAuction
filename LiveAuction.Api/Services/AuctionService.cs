

// using Microsoft.EntityFrameworkCore;
// using Microsoft.AspNetCore.SignalR;
// using LiveAuction.Api.Data;
// using LiveAuction.Api.Models;
// using LiveAuction.Api.Hubs; // וודא שהתיקייה קיימת

// namespace LiveAuction.Api.Services;

// public class AuctionService : IAuctionService
// {
//     private readonly AuctionDbContext _context;
//     private readonly IHubContext<AuctionHub> _hubContext;

//     public AuctionService(AuctionDbContext context, IHubContext<AuctionHub> hubContext)
//     {
//         _context = context;
//         _hubContext = hubContext;
//     }

//     // שליפת כל המכירות
//     public async Task<IEnumerable<Auction>> GetAllAuctionsAsync()
//     {
//         return await _context.Auctions
//             .OrderByDescending(a => a.EndTime)
//             .ToListAsync();
//     }

//     // שליפת מכירה ספציפית כולל היסטוריית הצעות
//     public async Task<Auction?> GetAuctionByIdAsync(int id)
//     {
//         return await _context.Auctions
//             .Include(a => a.Bids.OrderByDescending(b => b.CreatedAt))
//             .ThenInclude(b => b.User)
//             .FirstOrDefaultAsync(a => a.Id == id);
//     }

//     // הלוגיקה המרכזית: הגשת הצעה
//     public async Task<bool> PlaceBidAsync(int auctionId, int userId, decimal amount)
//     {
//         // שימוש ב-Transaction כדי להבטיח שהכל נשמר או כלום
//         using var transaction = await _context.Database.BeginTransactionAsync();
        
//         try
//         {
//             // שליפת המכירה
//             var auction = await _context.Auctions
//                 .FirstOrDefaultAsync(a => a.Id == auctionId);

//             // בדיקות חוקיות: קיום, סטטוס וזמן
//             if (auction == null || 
//                 auction.Status == AuctionStatus.Closed || 
//                 auction.EndTime < DateTime.UtcNow)
//             {
//                 return false;
//             }

//             // בדיקה שההצעה גבוהה מהמחיר הנוכחי
//             if (amount <= auction.CurrentHighPrice)
//             {
//                 return false;
//             }

//             // עדכון המכירה
//             auction.CurrentHighPrice = amount;
            
//             // עדכון ה-RowVersion ידנית עבור SQLite (טיפול במקביליות)
//             auction.RowVersion = Guid.NewGuid().ToString();

//             // יצירת רשומת ההצעה
//             var bid = new Bid
//             {
//                 AuctionId = auctionId,
//                 UserId = userId,
//                 Amount = amount,
//                 CreatedAt = DateTime.UtcNow
//             };

//             _context.Bids.Add(bid);

//             // שמירה לבסיס הנתונים - כאן תיזרק שגיאה אם מישהו הקדים אותנו (Concurrency)
//             await _context. ();
//             await transaction.CommitAsync();

//             // --- עדכון בזמן אמת דרך SignalR ---
//             // שולחים הודעה לכל מי שרשום לקבוצה של המכירה הזו
//             await _hubContext.Clients.Group($"Auction_{auctionId}")
//                 .SendAsync("ReceiveNewBid", new { 
//                     AuctionId = auctionId, 
//                     NewPrice = amount, 
//                     UserId = userId 
//                 });

//             return true;
//         }
//         catch (DbUpdateConcurrencyException)
//         {
//             // אם מישהו אחר עדכן את ה-RowVersion בדיוק באותה שנייה
//             await transaction.RollbackAsync();
//             return false;
//         }
//         catch (Exception)
//         {
//             await transaction.RollbackAsync();
//             throw;
//         }
//     }

//     // פונקציה לסגירת מכירות שפג תוקפן (עבור ה-Background Service)
//     public async Task CloseExpiredAuctionsAsync()
//     {
//         var now = DateTime.UtcNow;
//         var expiredAuctions = await _context.Auctions
//             .Where(a => a.Status == AuctionStatus.Active && a.EndTime <= now)
//             .ToListAsync();

//         if (expiredAuctions.Any())
//         {
//             foreach (var auction in expiredAuctions)
//             {
//                 auction.Status = AuctionStatus.Closed;
//                 auction.RowVersion = Guid.NewGuid().ToString();
//             }
//             await _context.SaveChangesAsync();
            
//             // ניתן להוסיף כאן הודעת SignalR שהמכירה נסגרה
//         }
//     }
// }

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using LiveAuction.Api.Data;
using LiveAuction.Api.Models;
using LiveAuction.Api.Hubs;

namespace LiveAuction.Api.Services;

public class AuctionService : IAuctionService
{
    private readonly AuctionDbContext _context;
    private readonly IHubContext<AuctionHub> _hubContext;

    public AuctionService(AuctionDbContext context, IHubContext<AuctionHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    // --- הפונקציה החדשה: יצירת מכירה ---
    public async Task<Auction> CreateAuctionAsync(AuctionCreateDto dto)
    {
        var auction = new Auction
        {
            Title = dto.Title,
            Description = dto.Description,
            StartingPrice = dto.StartingPrice,
            CurrentHighPrice = dto.StartingPrice, // מחיר נוכחי מתחיל ממחיר פתיחה
            ImageUrl = dto.ImageUrl,
            Status = AuctionStatus.Pending, // מתחיל כממתין בתור (לפי Enum שלך)
            CreatedAt = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddMinutes(dto.DurationMinutes),
            RowVersion = Guid.NewGuid().ToString()
        };

        _context.Auctions.Add(auction);
        await _context.SaveChangesAsync();

        // (אופציונלי) שליחת הודעה ב-SignalR שהתווסף מוצר לתור
        await _hubContext.Clients.All.SendAsync("AuctionAdded", auction);

        return auction;
    }

    public async Task<IEnumerable<Auction>> GetAllAuctionsAsync()
    {
        return await _context.Auctions
            .OrderBy(a => a.Status) // מציג קודם את הפעילים
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Auction?> GetAuctionByIdAsync(int id)
    {
        return await _context.Auctions
            .Include(a => a.Bids.OrderByDescending(b => b.CreatedAt))
            .ThenInclude(b => b.User)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<bool> PlaceBidAsync(int auctionId, int userId, decimal amount)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var auction = await _context.Auctions.FirstOrDefaultAsync(a => a.Id == auctionId);

            if (auction == null || 
                auction.Status != AuctionStatus.Active || // רק על פעיל אפשר להציע
                auction.EndTime < DateTime.UtcNow)
            {
                return false;
            }

            if (amount <= auction.CurrentHighPrice)
            {
                return false;
            }

            auction.CurrentHighPrice = amount;
            auction.RowVersion = Guid.NewGuid().ToString();

            var bid = new Bid
            {
                AuctionId = auctionId,
                UserId = userId,
                Amount = amount,
                CreatedAt = DateTime.UtcNow
            };

            _context.Bids.Add(bid);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _hubContext.Clients.Group($"Auction_{auctionId}")
                .SendAsync("ReceiveNewBid", new { 
                    AuctionId = auctionId, 
                    NewPrice = amount, 
                    UserId = userId 
                });

            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();
            return false;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task CloseExpiredAuctionsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredAuctions = await _context.Auctions
            .Where(a => a.Status == AuctionStatus.Active && a.EndTime <= now)
            .ToListAsync();

        if (expiredAuctions.Any())
        {
            foreach (var auction in expiredAuctions)
            {
                auction.Status = AuctionStatus.Closed;
                auction.RowVersion = Guid.NewGuid().ToString();
            }
            await _context.SaveChangesAsync();
            
            // הודעה לכולם שהסטטוס השתנה
            await _hubContext.Clients.All.SendAsync("AuctionsUpdated");
        }
    }
}