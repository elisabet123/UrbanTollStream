using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using EnrichmentService;
using EnrichmentService.Services;
using SharedTypes;
using SharedTypes.EventHub;

var builder = Host.CreateApplicationBuilder(args);
var configuration = builder.Configuration;
var eventHubConnectionString = configuration.GetConnectionString("eventhub")!;
builder.Services
    .AddSingleton(new DetectionConsumer(new EventHubConsumerClient(EventHubNames.EventHubConsumerGroupEnrichment, eventHubConnectionString, EventHubNames.EventHubName)))
    .AddSingleton(new DetectionProducer(new EventHubProducerClient(eventHubConnectionString, EventHubNames.EventHubName)))
    .AddSingleton(new OwnerService())
    .AddSingleton(new FeeService());
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
