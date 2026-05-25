using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Cosmos;
using SharedTypes;

namespace DetectionDatabase;

public class DetectionDatabase(string connectionString)
{
    private static readonly string DatabaseName = "signalsdb";
    private static readonly string ContainerName = "signals";

    private readonly Lazy<Task<Container>> _lazyContainer = new(async () =>
    {
        var cosmosClient = new CosmosClient(connectionString.Replace("https", "http"), new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            LimitToEndpoint = true
        });
        await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName);
        var database = cosmosClient.GetDatabase(DatabaseName);
        var response = await database.CreateContainerIfNotExistsAsync(ContainerName, "/CameraId");

        return response.Container;
    });

    public async Task<Detection> StoreSignal(Signal signal)
    {
        // TODO deterministic detection ID generation based on signal content
        var detectionId = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes($"{signal.CameraId}{signal.ImageUrl}")).Take(20).ToArray());
        var detection = new Detection(detectionId, signal.CameraId, signal.Timestamp, signal.ImageUrl, signal.VehicleId, signal.Confidence);
        var signalDocument = new SignalDocument(Guid.NewGuid(), signal.CameraId, detection);
        
        var container = await _lazyContainer.Value;
        await container.CreateItemAsync(signalDocument, new PartitionKey(signal.CameraId.ToString()));
        return detection;
    }
}

// TODO move to repository
public record SignalDocument(Guid id, Guid CameraId, Detection Detection);