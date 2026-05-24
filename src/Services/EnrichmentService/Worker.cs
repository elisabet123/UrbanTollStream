using System.Transactions;
using Azure.Messaging.EventHubs.Consumer;
using SharedTypes;
using SharedTypes.EventHub;
using SharedTypes.Messages;

namespace EnrichmentService;

public class Worker(ILogger<Worker> logger, DetectionConsumer detectionConsumer) : BackgroundService
{
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        detectionConsumer.On( (DetectionCreated detectionCreated, CancellationToken _) =>
        {
            logger.LogInformation("Received DetectionCreated event with ID: {DetectionId}", detectionCreated.Detection.DetectionId);
            return Task.CompletedTask;
        });
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await detectionConsumer.Start(stoppingToken);
        }
    }
}
