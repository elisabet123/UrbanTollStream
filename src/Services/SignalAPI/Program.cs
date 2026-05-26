using Azure.Messaging.EventHubs.Producer;
using Microsoft.AspNetCore.Mvc;
using SharedTypes;
using SharedTypes.EventHub;
using SharedTypes.Messages;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var cosmosConnectionString = configuration.GetConnectionString("cosmos")!;
var eventHubConnectionString = configuration.GetConnectionString("eventhub")!;

builder.Services.AddSingleton(new DetectionDatabase.DetectionDatabase(cosmosConnectionString))
    .AddSingleton(new DetectionProducer(new EventHubProducerClient(eventHubConnectionString, EventHubNames.EventHubName)));

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapDelete("/detections/{id}", async (string id, [FromServices] DetectionDatabase.DetectionDatabase database, [FromServices] DetectionProducer producerClient) =>
{
    var result = await database.GetDetectionInfo(id);
    if (result == null)
    {
        return Results.NotFound();
    }
    var signal = new Signal(CameraId: result.Value.cameraId, ImageUrl: result.Value.imageUrl, IsDeleted: true);
    var detection = await database.StoreSignal(signal);
    await producerClient.SendAsync(new DetectionDeleted(detection.DetectionId));
    return Results.Ok();
});
app.Run();
