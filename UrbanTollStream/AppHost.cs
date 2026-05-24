using SharedTypes;

var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure Resources
var cosmos = builder
    .AddAzureCosmosDB("cosmos")
    .RunAsEmulator(opts => opts.WithImageTag("vnext-EN20251022"))
    .WithEndpoint(endpointName: "data-explorer", endpoint =>
    {
        endpoint.UriScheme = "http";
        endpoint.TargetPort = 1234;
        endpoint.Port = 1234;
    })
    .WithUrls(context =>
    {
        var url = context.Urls.FirstOrDefault(u => u.Endpoint?.EndpointName == "data-explorer");
#pragma warning disable IDE0031 // Use null propagation (IDE0031)
        if (url is not null)
#pragma warning restore IDE0031
        {
            url.DisplayText = "Data Explorer";
        }
    });

var eventHub = builder
    .AddAzureEventHubs(EventHubNames.EventHub)
    .RunAsEmulator();
var detectionEvents = eventHub.AddHub(EventHubNames.EventHubName);
detectionEvents.AddConsumerGroup(EventHubNames.EventHubConsumerGroupEnrichment);
detectionEvents.AddConsumerGroup(EventHubNames.EventHubConsumerGroupAggregation);

var postgres = builder
    .AddPostgres("postgres")
    .AddDatabase("urbantollstream");

// API Services
_ = builder
    .AddProject<Projects.IngestionAPI>("ingestionapi")
    .WithReference(cosmos)
    .WithReference(eventHub)
    .WithHttpEndpoint(name: "ingestion-http");

_ = builder
    .AddProject<Projects.SignalAPI>("signalapi")
    .WithReference(cosmos)
    .WithReference(eventHub)
    .WithHttpEndpoint(name: "signal-http");

var feeService = builder
    .AddProject<Projects.FeeService>("feeservice")
    .WithHttpEndpoint(name: "fee-http");

var ownershipService = builder
    .AddProject<Projects.OwnershipService>("ownershipservice");

// Worker Services
_ = builder
    .AddProject<Projects.EnrichmentService>("enrichmentservice")
    .WithReference(feeService)
    .WithReference(ownershipService)
    .WithReference(eventHub);

_ = builder
    .AddProject<Projects.AggregationService>("aggregationservice")
    .WithReference(eventHub)
    .WithReference(postgres);

_ = builder
    .AddProject<Projects.BillingService>("billingservice")
    .WithReference(postgres);

builder.Build().Run();