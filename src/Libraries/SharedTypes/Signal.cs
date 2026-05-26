namespace SharedTypes;

public record Signal(
    // Can not be changed
    Guid CameraId,
    // Can not be changed
    string ImageUrl,
    DateTime? Timestamp = null,
    string? VehicleId = null,
    double? Confidence = null,
    bool IsDeleted = false
);
