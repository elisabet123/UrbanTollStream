using Microsoft.Azure.Cosmos;
using SharedTypes;

namespace DetectionDatabase;

public class DetectionDatabase(CosmosClient cosmosClient)
{
    public static DetectionDatabase Create(string connectionString)
    {
        var cosmosClient = new CosmosClient(connectionString.Replace("https", "http"), new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            LimitToEndpoint = true
        });
        return new DetectionDatabase(cosmosClient);
    }

    public async Task<Detection> StoreSignal(Signal signal)
    {
        // TODO deterministic detection ID generation based on signal content
        var detectionId = Guid.NewGuid();
        var detection = new Detection(detectionId, signal.CameraId, signal.Timestamp, signal.ImageUrl, signal.VehicleId, signal.Confidence);
        var signalDocument = new SignalDocument(Guid.NewGuid(), detection);
    
        // TODO setup database and container somewhere reasonable
        // TODO repository pattern and better error handling
        await cosmosClient.CreateDatabaseIfNotExistsAsync("signalsdb");
        var database = cosmosClient.GetDatabase("signalsdb");
        await database.CreateContainerIfNotExistsAsync("signals", "/CameraId");
        var container = database.GetContainer("signals");
    
        await container.CreateItemAsync(signalDocument, new PartitionKey(signal.CameraId.ToString()));
        return detection;
    }
}

// TODO move to repository
public record SignalDocument(Guid id, Detection Detection);