using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using IngestionAPI;
using IngestionAPI.Models;
using Microsoft.AspNetCore.Mvc;
using SharedTypes.Messages;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var cosmosConnectionString = configuration.GetConnectionString("cosmos")!;
var eventHubConnectionString = configuration.GetConnectionString("eventhub")!;

builder.Services.AddSingleton(DetectionDatabase.DetectionDatabase.Create(cosmosConnectionString))
    .AddSingleton(new EventHubProducerClient(eventHubConnectionString, "detectionevents"));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/signal", async ([FromServices] ILogger<Program> logger, [FromServices] DetectionDatabase.DetectionDatabase database, [FromServices] EventHubProducerClient producerClient, SignalDto signalDto) =>
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
    await producerClient.SendAsync([message.ToEventData()]);
    
    var anotherEvent = new EventData(new BinaryData($"Another event for detection {detection.DetectionId}"))
    {
        ContentType = "text/plain",
        Properties =
        {
            {
                "messageType", "AnotherEvent"
            }
        }
    };
    await producerClient.SendAsync([anotherEvent]);
    return Results.Ok();
});

app.Run();