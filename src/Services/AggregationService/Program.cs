using AggregationService;
using Azure.Messaging.EventHubs.Consumer;
using BillingDatabase;
using SharedTypes;
using SharedTypes.EventHub;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var configuration = builder.Configuration;
var eventHubConnectionString = configuration.GetConnectionString("eventhub")!;
var cosmosConnectionString = configuration.GetConnectionString("cosmos")!;

builder.Services
    .AddSingleton(new EventHubConsumerClient(EventHubNames.EventHubConsumerGroupEnrichment, eventHubConnectionString, EventHubNames.EventHubName))
    .AddSingleton<DetectionConsumer>()
    .AddSingleton(new DailyChargeDb(cosmosConnectionString));

var host = builder.Build();
host.Run();