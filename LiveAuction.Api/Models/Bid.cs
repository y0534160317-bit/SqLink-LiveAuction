using System.ComponentModel.DataAnnotations;

namespace LiveAuction.Api.Models;

public class Bid
{
    public int Id { get; set; }
    public int AuctionId { get; set; }
    public Auction? Auction { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}