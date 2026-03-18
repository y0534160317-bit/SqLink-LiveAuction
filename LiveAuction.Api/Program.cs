

using Microsoft.EntityFrameworkCore;
using LiveAuction.Api.Data;
using LiveAuction.Api.Services;
using LiveAuction.Api.Hubs;
using LiveAuction.Api.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// 1. חיבור לבסיס הנתונים SQLite
builder.Services.AddDbContext<AuctionDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. רישום ה-Service של המכירות
builder.Services.AddScoped<IAuctionService, AuctionService>();

// 3. הוספת SignalR לעדכונים בזמן אמת
builder.Services.AddSignalR();

// 4. הוספת ה-Worker שרץ ברקע וסוגר מכירות שפג תוקפן
builder.Services.AddHostedService<AuctionClosingWorker>();

// 5. הגדרות Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 6. הגדרת CORS (קריטי כדי שה-Angular יוכל לדבר עם ה-API)
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAngular", policy => policy
        .WithOrigins("http://localhost:4200") // הכתובת של אנגולר
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()); // חובה עבור SignalR
});

var app = builder.Build();

// --- Middleware ---
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");
app.UseHttpsRedirection();

// --- Endpoints ---

// שליפת כל המכירות
app.MapGet("/api/auctions", async (IAuctionService service) => 
    Results.Ok(await service.GetAllAuctionsAsync()));

// שליפת מכירה ספציפית
app.MapGet("/api/auctions/{id}", async (int id, IAuctionService service) => {
    var auction = await service.GetAuctionByIdAsync(id);
    return auction is not null ? Results.Ok(auction) : Results.NotFound();
});

app.MapPost("/api/auctions", async (AuctionCreateDto dto, IAuctionService service) => 
{
    var auction = await service.CreateAuctionAsync(dto);
    return Results.Created($"/api/auctions/{auction.Id}", auction);
});

// הגשת הצעת מחיר
app.MapPost("/api/auctions/{id}/bid", async (int id, BidRequest request, IAuctionService service) => {
    var success = await service.PlaceBidAsync(id, request.UserId, request.Amount);
    return success ? Results.Ok() : Results.BadRequest("Bid rejected (Price too low or Auction closed)");
});

// --- מיפוי ה-SignalR Hub ---
app.MapHub<AuctionHub>("/auctionhub");

// --- Seed Data (אופציונלי) ---
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
    db.Database.EnsureCreated();
    // כאן אפשר להוסיף את הקוד של ה-Seed שכתבנו קודם אם ה-DB ריק
}

app.Run();

// DTO פשוט
public record BidRequest(int UserId, decimal Amount);
public record AuctionCreateDto(string Title, string Description, decimal StartingPrice, string ImageUrl, int DurationMinutes);