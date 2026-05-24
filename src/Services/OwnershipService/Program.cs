using Microsoft.AspNetCore.Mvc;
using SharedTypes;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/owner", ([FromQuery] string vehicleId, [FromQuery] DateTime date) =>
{
    // TODO query transportstyrelsen about the owner of vehicles at certain dates and keep records in a database for faster retrieval
    if (vehicleId == "ABC123" && date < DateTime.UtcNow.AddDays(-30))
    {
        return new Owner("Jane", "Smith");
    }

    return new Owner("John", "Doe");
}).WithName("GetOwner");

app.Run();
