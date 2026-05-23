using IngestionAPI.Models;
using SharedTypes;

namespace IngestionAPI;

public static class Extensions
{
    extension (DetectionDatabase.DetectionDatabase database)
    {
        public async Task<Detection> StoreSignal(SignalDto signalDto)
        {
            var signal = new Signal(signalDto.CameraId, signalDto.Timestamp, signalDto.ImageUrl, signalDto.VehicleId, signalDto.Confidence);
            return await database.StoreSignal(signal);
        }
        
    }
}