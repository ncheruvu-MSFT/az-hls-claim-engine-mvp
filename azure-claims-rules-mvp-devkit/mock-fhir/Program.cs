
using System.Text.Json;
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/Claim", async (HttpRequest req) =>
{
    using var reader = new StreamReader(req.Body);
    var payload = await reader.ReadToEndAsync();
    var id = Guid.NewGuid().ToString();
    var response = new { resourceType = "Claim", id = id, created = DateTime.UtcNow.ToString("o") };
    return Results.Json(response);
});

app.MapGet("/health", () => "ok");
app.Run("http://localhost:7072");
