namespace SharedTypes;

public record Detection(Guid DetectionId, Guid CameraId, DateTime Timestamp, string ImageUrl, string VehicleId, double Confidence);