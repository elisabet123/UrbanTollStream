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
            var owner = ownerService.GetOwner(detectionCreated.detection.VehicleId, detectionCreated.detection.Timestamp);
            var fee = feeService.GetFeeAsync(detectionCreated.detection.CameraId, detectionCreated.detection.Timestamp);
            await Task.WhenAll(owner, fee);
            logger.LogDebug("Received DetectionCreated event with ID: {DetectionId}, enriched with fee {Fee} and owner {Owner}", detectionCreated.detection.DetectionId, fee.Result, owner.Result);
            var enrichedDetection = new DetectionEnriched(EnrichedDetection.FromDetection(
                detectionCreated.detection,
                fee.Result, 
                owner.Result
            ));
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