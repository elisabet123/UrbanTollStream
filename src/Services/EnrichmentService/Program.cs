using Azure.Messaging.EventHubs.Consumer;
using EnrichmentService;
using SharedTypes;
using SharedTypes.EventHub;

var builder = Host.CreateApplicationBuilder(args);
var configuration = builder.Configuration;
var eventHubConnectionString = configuration.GetConnectionString("eventhub")!;
builder.Services.AddSingleton(new DetectionConsumer(new EventHubConsumerClient(EventHubNames.EventHubConsumerGroupEnrichment, eventHubConnectionString, EventHubNames.EventHubName)));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
