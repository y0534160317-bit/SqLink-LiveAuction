using System.ComponentModel.DataAnnotations;

namespace LiveAuction.Api.Models;

public class User
{
    public int Id { get; set; }
    [Required]
    public string Username { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    public List<Bid> Bids { get; set; } = new();
}