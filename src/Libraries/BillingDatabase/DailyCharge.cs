using SharedTypes;

namespace BillingDatabase;

public record DailyCharge(DateTime Date, string VehicleId, double TotalCharge, Dictionary<string, EnrichedDetection> Detections);
