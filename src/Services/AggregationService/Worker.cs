using BillingDatabase;
using SharedTypes;
using SharedTypes.EventHub;
using SharedTypes.Messages;

namespace AggregationService;

public class Worker(ILogger<Worker> logger, DetectionConsumer detectionConsumer, DailyChargeDb dailyChargeDb, DetectionLookupDb lookupDb) : BackgroundService
{
    
    // TODO cancellation tokens on everything and stuff should be transactional
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        detectionConsumer.On(async (DetectionEnriched detectionEvent, CancellationToken _) =>
        {   
            logger.LogDebug("Received DetectionEnriched event with ID: {DetectionId}", detectionEvent.detection.DetectionId);
            var detection = detectionEvent.detection;
            var dailyCharge = await dailyChargeDb.GetDailyCharge(detection.Timestamp, detection.VehicleId);
            var detectionInfo = await lookupDb.GetDetectionInfo(detection.DetectionId);

            if (dailyCharge is null)
            {
                // this is a first for this vehicle on this day
                var newCharge = new DailyCharge(detection.Timestamp.Date, detection.VehicleId, detection.Fee.Amount, new Dictionary<string, EnrichedDetection> { { detection.DetectionId, detection } });
                await StoreCharge(newCharge, detection.DetectionId);
            }
            else
            {
                // add detection to vehicle's daily charge
                var oldDetections = dailyCharge.Detections;
                var newDetections = new Dictionary<string, EnrichedDetection>(oldDetections)
                {
                    [detection.DetectionId] = detection
                };
                var newTotalCharge = CalculateTotalCharge(newDetections.Values.ToArray());
                var updatedDailyCharge = dailyCharge with { Detections = newDetections, TotalCharge = newTotalCharge };
                await StoreCharge(updatedDailyCharge, detection.DetectionId);
            }
            if (detectionInfo is not null)
            {
                var (date, vehicleId) = detectionInfo.Value;
                if (!vehicleId.Equals(dailyCharge?.VehicleId) || dailyCharge.Date != date)
                {
                    // we have seen this detection before, but not for this vehicle+day
                    await RemoveDetectionFromDailyCharge(date, vehicleId, detection.DetectionId);
                }
            }
        });
        
        detectionConsumer.On(async (DetectionDeleted detectionEvent, CancellationToken _) =>
        {
            var detectionId = detectionEvent.DetectionId;
            logger.LogDebug("Received DetectionDeleted event with ID: {DetectionId}", detectionId);
            var detectionInfo = await lookupDb.GetDetectionInfo(detectionId);
            if (detectionInfo is null)
            {
                // this can happen if the detection messages are out of order (i.e. we receive the deletion before the enrichment)
                // TODO we might want to retry or replay the deletion, maybe store it somewhere if we later receive an enrichment for a detection we previously received a deletion for
                logger.LogInformation("Received DetectionDeleted event for detection ID {DetectionId} but no lookup info found, skipping deletion.", detectionId);
                return;
            }
            var (date, vehicleId) = detectionInfo.Value;
            await RemoveDetectionFromDailyCharge(date, vehicleId, detectionId);
        });
        return base.StartAsync(cancellationToken);
    }

    private async Task RemoveDetectionFromDailyCharge(DateTime date, string vehicleId, string detectionId)
    {
        var dailyCharge = await dailyChargeDb.GetDailyCharge(date, vehicleId);
        if (dailyCharge is null || !dailyCharge.Detections.ContainsKey(detectionId))
        {
            // this should not happen, if we have a lookup entry for the detection we should have a daily charge entry as well, but just in case
            logger.LogWarning("Received DetectionDeleted event for detection ID {DetectionId} but no corresponding daily charge detection entry found, skipping deletion.", detectionId);
            return;
        }
        var oldDetections = dailyCharge.Detections;
        var newDetections = new Dictionary<string, EnrichedDetection>(oldDetections);
        newDetections.Remove(detectionId);
        var newTotalCharge = CalculateTotalCharge(newDetections.Values.ToArray());
        var updatedDailyCharge = dailyCharge with { Detections = newDetections, TotalCharge = newTotalCharge };

        await dailyChargeDb.SetDailyCharge(updatedDailyCharge);
    }

    private async Task StoreCharge(DailyCharge dailyCharge, string detectionId)
    {
        await dailyChargeDb.SetDailyCharge(dailyCharge);
        await lookupDb.StoreDetectionInfo(detectionId, dailyCharge.Date, dailyCharge.VehicleId);
    }

    private double CalculateTotalCharge(EnrichedDetection[] dailyChargeDetections)
    {
        // TODO this is a placeholder, implement actual charge calculation logic
        // 60 minutes window, max amount per day etc
        // probably defer logic to its own class at least
        var maxPerDay = 30;
        var totalCharge = dailyChargeDetections.Sum(d => d.Fee.Amount);
        return Math.Min(totalCharge, maxPerDay);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await detectionConsumer.Start(stoppingToken);
    }
}