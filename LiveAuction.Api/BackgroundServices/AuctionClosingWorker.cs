using LiveAuction.Api.Services;
namespace LiveAuction.Api.BackgroundServices;
public class AuctionClosingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    public AuctionClosingWorker(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var auctionService = scope.ServiceProvider.GetRequiredService<IAuctionService>();
                await auctionService.CloseExpiredAuctionsAsync();
            }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
