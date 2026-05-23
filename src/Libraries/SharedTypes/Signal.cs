namespace SharedTypes;

public record Signal(
    Guid CameraId,
    DateTime Timestamp,
    string ImageUrl,
    string VehicleId,
    double Confidence
);
