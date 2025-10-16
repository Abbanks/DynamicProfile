using System.Text.Json;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddHttpClient();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

var app = builder.Build();
var logger = app.Logger;

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();

app.MapGet("/me", async (IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient();
    httpClient.Timeout = TimeSpan.FromSeconds(5);

    var email = Environment.GetEnvironmentVariable("EMAIL") ?? "your-email@example.com";
    var name = Environment.GetEnvironmentVariable("NAME") ?? "Your Full Name";
    var stack = Environment.GetEnvironmentVariable("STACK") ?? "Stack";

    var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

    try
    {
        var response = await httpClient.GetAsync("https://catfact.ninja/fact");
        response.EnsureSuccessStatusCode();

        var contentStream = await response.Content.ReadAsStreamAsync();
        using var jsonDoc = await JsonDocument.ParseAsync(contentStream);
        string catFact = jsonDoc.RootElement.GetProperty("fact").GetString() ?? "No fact available";

        var result = new
        {
            status = "success",
            user = new
            {
                email,
                name,
                stack
            },
            timestamp,
            fact = catFact
        };

        return Results.Json(result, statusCode: 200);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error fetching cat fact");
        var result = new
        {
            status = "success",
            user = new
            {
                email,
                name,
                stack
            },
            timestamp,
            fact = "Cat facts are currently unavailable. Please try again later."
        };

        return Results.Json(result, statusCode: 200);
    }
});

app.Run();
