namespace IngestionAPI.Models;

// TODO validation?
public record SignalDto(
    Guid CameraId,
    DateTime Timestamp,
    string ImageUrl,
    string VehicleId,
    double Confidence
);