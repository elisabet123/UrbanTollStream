using BillingDatabase;
using SharedTypes;
using SharedTypes.EventHub;
using SharedTypes.Messages;

namespace AggregationService;

public class Worker(ILogger<Worker> logger, DetectionConsumer detectionConsumer, DailyChargeDb dailyChargeDb) : BackgroundService
{
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        detectionConsumer.On(async (DetectionEnriched detectionEvent, CancellationToken _) =>
        {   
            logger.LogDebug("Received DetectionEnriched event with ID: {DetectionId}", detectionEvent.detection.DetectionId);
            var detection = detectionEvent.detection;
            var dailyCharge = await dailyChargeDb.GetDailyCharge(detection.Timestamp, detection.VehicleId);

            if (dailyCharge is null)
            {
                dailyCharge = new DailyCharge(detection.Timestamp.Date, detection.VehicleId, detection.Fee.Amount, new Dictionary<string, EnrichedDetection> { { detection.DetectionId, detection } });
                await dailyChargeDb.SetDailyCharge(dailyCharge);
            }
            else
            {
                var oldDetections = dailyCharge.Detections;
                var newDetections = new Dictionary<string, EnrichedDetection>(oldDetections)
                {
                    [detection.DetectionId] = detection
                };
                var newTotalCharge = CalculateTotalCharge(newDetections.Values.ToArray());
                var updatedDailyCharge = dailyCharge with { Detections = newDetections, TotalCharge = newTotalCharge };
                await dailyChargeDb.SetDailyCharge(updatedDailyCharge);
            }
        });
        return base.StartAsync(cancellationToken);
    }

    private double CalculateTotalCharge(EnrichedDetection[] dailyChargeDetections)
    {
        // TODO this is a placeholder, implement actual charge calculation logic
        // 60 minutes window, max amount per day etc
        var maxPerDay = 100;
        var totalCharge = dailyChargeDetections.Sum(d => d.Fee.Amount);
        return Math.Min(totalCharge, maxPerDay);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await detectionConsumer.Start(stoppingToken);
    }
}