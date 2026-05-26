namespace SharedTypes;

public record EnrichedDetection(string DetectionId, Guid CameraId, DateTime Timestamp, string ImageUrl, string VehicleId, double Confidence, Fee Fee, string Owner);