using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using EnrichmentService;
using EnrichmentService.Services;
using SharedTypes;
using SharedTypes.EventHub;

var builder = Host.CreateApplicationBuilder(args);
var configuration = builder.Configuration;
var eventHubConnectionString = configuration.GetConnectionString("eventhub")!;

builder.Services.ConfigureHttpClientDefaults(http =>
{
    // Turn on service discovery by default
    http.AddServiceDiscovery();
});

builder.Services
    .AddServiceDiscovery()
    .AddSingleton(new EventHubConsumerClient(EventHubNames.EventHubConsumerGroupEnrichment, eventHubConnectionString, EventHubNames.EventHubName))
    .AddSingleton<DetectionConsumer>()
    .AddSingleton(new EventHubProducerClient(eventHubConnectionString, EventHubNames.EventHubName))
    .AddSingleton<DetectionProducer>()
    .AddSingleton<OwnerService>()
    .AddSingleton<FeeService>()
    .AddHttpClient(nameof(OwnerService), client => client.BaseAddress = new("http://ownershipservice"));
builder.Services.AddHttpClient(nameof(FeeService), client => client.BaseAddress = new("http://feeservice"));
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
