using Tanks.Server.Services;
using Tanks.Server.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMagicOnion();

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register custom services
builder.Services.AddSingleton<IPlayerManagerService, PlayerManagerService>();
builder.Services.AddSingleton<IGameStateService, GameStateService>();

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