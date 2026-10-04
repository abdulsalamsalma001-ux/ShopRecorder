using ShopRecorder.Components;
using ShopRecorder.Data;

var builder = WebApplication.CreateBuilder(args);

// Blazor Web App — interactive server render mode for the whole app (monolith, no separate API).
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Single shared SQLite connection for the whole app.
builder.Services.AddSingleton<ISQLiteService, SQLiteService>();

var app = builder.Build();

// Hosting platforms (Azure App Service, Render, etc.) may tell us the port via the PORT
// env variable. Locally, ASPNETCORE_URLS / launchSettings are used instead.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    app.Urls.Add($"http://*:{port}");
}

// Create the SQLite database (if it does not exist) and seed sample products on first run.
await app.Services.GetRequiredService<ISQLiteService>().Init();

app.UseStaticFiles();
app.UseAntiforgery();

// Simple health endpoint for platform health checks (Render/Azure).
app.MapGet("/health", () => Results.Ok(new { status = "ok", app = "ShopRecorder" }));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
