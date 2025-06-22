using Tanks.Server.Services;
using Tanks.Server.Components;
using Tanks.Server.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMagicOnion();

// Add Entity Framework
builder.Services.AddDbContext<TankGameDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 4, 5))
    )
);

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register custom services
builder.Services.AddSingleton<IPlayerManagerService, PlayerManagerService>();
builder.Services.AddSingleton<IGameStateService, GameStateService>();
builder.Services.AddScoped<IUserService, UserService>();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

// MagicOnion service mapping (gRPC endpoint)
app.MapMagicOnionService();

// Map Blazor components
app.MapRazorComponents<Tanks.Server.Components.App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/api", () => "Tanks Game Server API - Admin panel available at /");

app.Run();