namespace SharedTypes;

public record EnrichedDetection(string DetectionId, Guid CameraId, DateTime Timestamp, string ImageUrl, string VehicleId, double Confidence, Fee Fee, string Owner)
{
    public static EnrichedDetection FromDetection(Detection detection, Fee fee, string owner)
    {
        return new EnrichedDetection(detection.DetectionId, detection.CameraId, detection.Timestamp, detection.ImageUrl, detection.VehicleId, detection.Confidence, fee, owner);
    }
}