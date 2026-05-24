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
app.MapGet("/fee", (Guid cameraId, DateTime date) =>
{
    // TODO implement actual fee logic based on cameraId and date, add more conditions
    if (date.Hour < 6 || date.Hour > 22)
    {
        return new Fee(0, "20260524", ["Off-peak hours"]);
    }

    if (cameraId == Guid.Parse("123e4567-e89b-12d3-a456-426614174000"))
    {
        return new Fee(20, "20260524", ["Zone A"]);
    }
    
    var fee = new Fee(10, "20260524", ["default fee"]);
    return fee;
});

// TODO API for updating fee rules, e.g. for different zones, time-based fees, etc.
app.Run();
