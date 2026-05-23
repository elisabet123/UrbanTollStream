using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using IngestionAPI.Models;
using Microsoft.AspNetCore.Mvc;
using IngestionAPI;
using SharedTypes.Messages;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(_ =>
    {
        var cosmosConnectionString = builder.Configuration.GetConnectionString("cosmos")!;
        return DetectionDatabase.DetectionDatabase.Create(cosmosConnectionString);
    })
    .AddSingleton(_ =>
    {
        var connectionString = builder.Configuration.GetConnectionString("eventhub")!;
        return new EventHubProducerClient(connectionString, "detectionevents");
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/signal", async ([FromServices] ILogger<Program> logger,[FromServices] DetectionDatabase.DetectionDatabase database, [FromServices] EventHubProducerClient producerClient, SignalDto signalDto) =>
{
    // TODO configurable confidence threshold
    if (signalDto.Confidence < 0.5)
    {
        logger.LogInformation($"Low confidence signal received for vehicle {signalDto.VehicleId} at camera {signalDto.CameraId}. Ignoring.");
        return Results.BadRequest("Signal confidence too low.");
    }

    var detection = await database.StoreSignal(signalDto);
    logger.LogDebug($"Signal received for vehicle {signalDto.VehicleId} at camera {signalDto.CameraId} with confidence {signalDto.Confidence}. Stored in Cosmos DB.");

    var message = new DetectionCreated(detection);
    var newEvent = new EventData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)))
    {
        ContentType = "application/json",
        Properties =
        {
            { "messageType", typeof(DetectionCreated).FullName }
        }
    };
    await producerClient.SendAsync([newEvent]);
    return Results.Ok();
});

app.Run();