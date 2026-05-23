using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(sp =>
{
    var cosmosConnectionString = builder.Configuration.GetConnectionString("cosmos")!;
    var cosmos = new CosmosClient(cosmosConnectionString.Replace("https", "http"), new CosmosClientOptions
    {
        ConnectionMode = ConnectionMode.Gateway,
        LimitToEndpoint = true
    });

    return cosmos;
});

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

app.MapPost("/signal", async ([FromServices] CosmosClient cosmosClient, SignalDTO signalDto) =>
{
    // TODO configurable confidence threshold
    if (signalDto.Confidence < 0.5)
    {
        Console.WriteLine($"Low confidence signal received for vehicle {signalDto.VehicleId} at camera {signalDto.CameraId}. Ignoring.");
        return Results.BadRequest("Signal confidence too low.");
    }
    // TODO deterministic detection ID generation based on signal content
    var signalDocument = new SignalDocument(Guid.NewGuid(), Guid.NewGuid(), signalDto.CameraId, signalDto.Timestamp, signalDto.ImageUrl, signalDto.VehicleId, signalDto.Confidence);
    
    // TODO setup database and container somewhere reasonable
    // TODO repository pattern and better error handling
    await cosmosClient.CreateDatabaseIfNotExistsAsync("signalsdb");
    var database = cosmosClient.GetDatabase("signalsdb");
    await database.CreateContainerIfNotExistsAsync("signals", "/CameraId");
    var container = database.GetContainer("signals");
    
    await container.CreateItemAsync(signalDocument, new PartitionKey(signalDto.CameraId.ToString()));
    Console.WriteLine($"Signal received for vehicle {signalDto.VehicleId} at camera {signalDto.CameraId} with confidence {signalDto.Confidence}. Stored in Cosmos DB.");
    return Results.Ok();
});

app.Run();

// TODO move to separate file and add validation attributes
public class SignalDTO
{
    public Guid CameraId { get; init; }
    public DateTime Timestamp { get; init; }
    public string ImageUrl { get; init; }
    public string VehicleId { get; init; }
    public double Confidence { get; init; }
}

// TODO move to repository
public record SignalDocument(Guid id, Guid DetectionId, Guid CameraId, DateTime Timestamp, string ImageUrl, string VehicleId, double Confidence);