using Microsoft.AspNetCore.SignalR;
namespace LiveAuction.Api.Hubs;
public class AuctionHub : Hub 
{
    public async Task JoinAuctionGroup(int auctionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Auction_{auctionId}");
    }
}