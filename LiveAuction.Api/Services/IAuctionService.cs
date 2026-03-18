

namespace LiveAuction.Api.Services;

// ה-DTO שמתאים לטופס באנגולר
public record AuctionCreateDto(string Title, string Description, decimal StartingPrice, string ImageUrl, int DurationMinutes);

public interface IAuctionService
{
    Task<IEnumerable<Auction>> GetAllAuctionsAsync();
    Task<Auction?> GetAuctionByIdAsync(int id);
    Task<bool> PlaceBidAsync(int auctionId, int userId, decimal amount);
    Task CloseExpiredAuctionsAsync();
    // הפונקציה החדשה ליצירת מוצר
    Task<Auction> CreateAuctionAsync(AuctionCreateDto dto);
}