namespace SharedTypes;

public record Detection(string DetectionId, Guid CameraId, DateTime Timestamp, string ImageUrl, string VehicleId, double Confidence);