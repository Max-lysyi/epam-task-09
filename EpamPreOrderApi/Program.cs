using EpamPreOrderApi.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add controllers, Swagger, and CORS
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// In-Memory database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("PreOrderDb"));

var app = builder.Build();

// Seed initial test data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.SeedData();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new { message = "EpamPreOrderApi is running", status = "Healthy" }));
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

var renderPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(renderPort))
{
    app.Urls.Add($"http://0.0.0.0:{renderPort}");
}

app.Run();
