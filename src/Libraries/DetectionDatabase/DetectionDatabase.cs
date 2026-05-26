using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Cosmos;
using SharedTypes;

namespace DetectionDatabase;

public class DetectionDatabase(string connectionString)
{
    private static readonly string DatabaseName = "signalsdb";
    private static readonly string ContainerName = "signals";
    private static readonly string LookupContainerName = "detectionlookup";

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
    
    private readonly Lazy<Task<Container>> _lazyLookupContainer = new(async () =>
    {
        var cosmosClient = new CosmosClient(connectionString.Replace("https", "http"), new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            LimitToEndpoint = true
        });
        await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName);
        var database = cosmosClient.GetDatabase(DatabaseName);
        var response = await database.CreateContainerIfNotExistsAsync(LookupContainerName, "/DetectionId");

        return response.Container;
    });

    // Assume imageurl is unique per camera
    private string GetDetectionId(Guid cameraId, string imageUrl) => Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes($"{cameraId}{imageUrl}")).Take(20).ToArray());

    public async Task<Detection> StoreSignal(Signal signal)
    {
        var detectionId = GetDetectionId(signal.CameraId, signal.ImageUrl);
        var detection = new Detection(detectionId, signal.CameraId, signal.ImageUrl, signal.Timestamp, signal.VehicleId, signal.Confidence);
        var signalDocument = new SignalDocument(Guid.NewGuid(), signal.CameraId.ToString(), detection);

        var container = await _lazyContainer.Value;
        await container.CreateItemAsync(signalDocument, new PartitionKey(signal.CameraId.ToString()));
        
        var lookupContainer = await _lazyLookupContainer.Value;
        var lookupDocument = new DetectionLookupDocument(detectionId, detectionId, signal.CameraId, signal.ImageUrl);
        await lookupContainer.UpsertItemAsync(lookupDocument, new PartitionKey(detectionId));
        
        return detection;
    }
    
    public async Task<(Guid cameraId, string imageUrl)?> GetDetectionInfo(string detectionId)
    {
        var container = await _lazyLookupContainer.Value;
        try
        {            
            var response = await container.ReadItemAsync<DetectionLookupDocument>(detectionId, new PartitionKey(detectionId));
            return (response.Resource.CameraId, response.Resource.VehicleId);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
    
    private record SignalDocument(Guid id, string CameraId, Detection Detection);
    private record DetectionLookupDocument(string id, string DetectionId, Guid CameraId, string VehicleId);
}

