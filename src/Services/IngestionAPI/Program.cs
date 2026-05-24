using Azure.Messaging.EventHubs.Producer;
using IngestionAPI;
using IngestionAPI.Models;
using Microsoft.AspNetCore.Mvc;
using SharedTypes;
using SharedTypes.EventHub;
using SharedTypes.Messages;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var cosmosConnectionString = configuration.GetConnectionString("cosmos")!;
var eventHubConnectionString = configuration.GetConnectionString("eventhub")!;

builder.Services.AddSingleton(DetectionDatabase.DetectionDatabase.Create(cosmosConnectionString))
    .AddSingleton(new DetectionProducer(new EventHubProducerClient(eventHubConnectionString, EventHubNames.EventHubName)));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/signal", async ([FromServices] ILogger<Program> logger, [FromServices] DetectionDatabase.DetectionDatabase database, [FromServices] DetectionProducer producerClient, SignalDto signalDto) =>
{
    // TODO configurable confidence threshold
    if (signalDto.Confidence < 0.5)
    {
        logger.LogInformation($"Low confidence signal received for vehicle {signalDto.VehicleId} at camera {signalDto.CameraId}. Ignoring.");
        return Results.BadRequest("Signal confidence too low.");
    }

    var detection = await database.StoreSignal(signalDto);
    logger.LogDebug($"Signal received for vehicle {signalDto.VehicleId} at camera {signalDto.CameraId} with confidence {signalDto.Confidence}. Stored in Cosmos DB.");

    await producerClient.SendAsync(new DetectionCreated(detection));

    return Results.Ok();
});

app.Run();