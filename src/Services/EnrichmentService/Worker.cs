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
            var detection = detectionCreated.detection;
            if (detection.Timestamp == null)
            {
                logger.LogWarning("Detected detection created with no timestamp, can not enrich. Detection ID: {DetectionId}", detection.DetectionId);
                return;
            }
            var timeStamp = detection.Timestamp.Value;

            if (detection.VehicleId == null)
            {
                logger.LogWarning("Detected detection created with no vehicle ID, can not enrich. Detection ID: {DetectionId}", detection.DetectionId);
                return;
            }
            
            var owner = ownerService.GetOwner(detection.VehicleId, timeStamp);
            var fee = feeService.GetFeeAsync(detection.CameraId, timeStamp);
            await Task.WhenAll(owner, fee);
            logger.LogDebug("Received DetectionCreated event with ID: {DetectionId}, enriched with fee {Fee} and owner {Owner}", detection.DetectionId, fee.Result, owner.Result);
            var enrichedDetection = new DetectionEnriched(new EnrichedDetection(
                detection.DetectionId,
                detection.CameraId,
                timeStamp,
                detection.ImageUrl,
                detection.VehicleId,
                detection.Confidence ?? 0,
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