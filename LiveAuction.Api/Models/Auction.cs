using System.ComponentModel.DataAnnotations;

namespace LiveAuction.Api.Models;

public enum AuctionStatus { Active, Closed }

public class Auction
{
    // public int Id { get; set; }
    
    // [Required]
    // public string Title { get; set; } = string.Empty;
    
    // public decimal StartingPrice { get; set; }
    
    // public decimal CurrentHighPrice { get; set; }
    
    // public DateTime EndTime { get; set; }
    
    // public AuctionStatus Status { get; set; } = AuctionStatus.Active;

    // [ConcurrencyCheck]
    // public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // public List<Bid> Bids { get; set; } = new();
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal CurrentHighPrice { get; set; }
    public decimal StartingPrice { get; set; }
    public DateTime EndTime { get; set; }
    public AuctionStatus Status { get; set; }

    // וודאי שכתוב string ולא byte[]
    public string RowVersion { get; set; } = Guid.NewGuid().ToString();

    public List<Bid> Bids { get; set; } = new();
}