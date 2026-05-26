namespace SharedTypes;

public record Detection(string DetectionId, Guid CameraId, string ImageUrl, DateTime? Timestamp = null, string? VehicleId = null, double? Confidence = null, bool IsDeleted = false);