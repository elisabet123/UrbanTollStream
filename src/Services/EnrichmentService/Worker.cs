using EnrichmentService.Services;
using SharedTypes;
using SharedTypes.EventHub;
using SharedTypes.Messages;

namespace EnrichmentService;

public class Worker(ILogger<Worker> logger, DetectionConsumer detectionConsumer, DetectionProducer detectionProducer, OwnerService ownerService, FeeService feeService) : BackgroundService
{
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        detectionConsumer.On(async (DetectionCreated detectionCreated, CancellationToken _) =>
        {
            var owner = ownerService.GetOwner(detectionCreated.Detection.VehicleId, detectionCreated.Detection.Timestamp);
            var fee = feeService.GetFeeAsync(detectionCreated.Detection.VehicleId, detectionCreated.Detection.Timestamp);
            await Task.WhenAll(owner, fee);
            logger.LogDebug("Received DetectionCreated event with ID: {DetectionId}, enriched with fee {Fee} and owner {Owner}", detectionCreated.Detection.DetectionId, fee.Result, owner.Result);
            var enrichedDetection = new DetectionEnriched(
                detectionCreated.Detection,
                new EnrichmentResult(fee.Result, owner.Result)
            );
            await detectionProducer.SendAsync(enrichedDetection);
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