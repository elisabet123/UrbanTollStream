var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure Resources
var cosmos = builder
    .AddAzureCosmosDB("cosmos")
    .RunAsEmulator(opts => opts.WithImageTag("vnext-preview"))
    .WithHttpEndpoint(name: "cosmos-explorer", port: 8081, targetPort: 8081);

var eventHub = builder
    .AddAzureEventHubs("eventhub")
    .RunAsEmulator();

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

_ = builder
    .AddProject<Projects.FeeService>("feeservice")
    .WithReference(eventHub)
    .WithHttpEndpoint(name: "fee-http");

_ = builder
    .AddProject<Projects.OwnershipService>("ownershipservice")
    .WithHttpEndpoint(name: "ownership-http");

// Worker Services
_ = builder
    .AddProject<Projects.ProcessingService>("processingservice")
    .WithReference(eventHub);

_ = builder
    .AddProject<Projects.AggregationService>("aggregationservice")
    .WithReference(eventHub)
    .WithReference(postgres);

_ = builder
    .AddProject<Projects.BillingService>("billingservice")
    .WithReference(postgres);

builder.Build().Run();