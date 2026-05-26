using Microsoft.Azure.Cosmos;

namespace BillingDatabase;

public class DetectionLookupDb(string connectionString)
{
    private static readonly string ContainerName = "detectionlookup";
    private static readonly string DatabaseName = "Billing";
    
    private readonly Lazy<Task<Container>> _lazyContainer = new(async () =>
    {
        var cosmosClient = new CosmosClient(connectionString.Replace("https", "http"), new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            LimitToEndpoint = true
        });
        await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName);
        var database = cosmosClient.GetDatabase(DatabaseName);
        var response = await database.CreateContainerIfNotExistsAsync(ContainerName, "/DetectionId");
        return response.Container;
    });
    
    public async Task<(DateTime date, string vehicleId)?> GetDetectionInfo(string detectionId)
    {
        var container = await _lazyContainer.Value;
        try
        {
            var response = await container.ReadItemAsync<DetectionLookupDocument>(detectionId, new PartitionKey(detectionId));
            return (response.Resource.Date, response.Resource.VehicleId);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task StoreDetectionInfo(string detectionId, DateTime date, string vehicleId)
    {
        var container = await _lazyContainer.Value;
        var document = new DetectionLookupDocument(detectionId, detectionId, date, vehicleId);
        await container.UpsertItemAsync(document, new PartitionKey(detectionId));
    }

    private record DetectionLookupDocument(string id, string DetectionId, DateTime Date, string VehicleId);
}