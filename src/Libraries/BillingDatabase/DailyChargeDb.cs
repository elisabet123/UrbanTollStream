using Microsoft.Azure.Cosmos;

namespace BillingDatabase;

public class DailyChargeDb(string connectionString)
{
    private static readonly string ContainerName = "dailycharges";
    private static readonly string DatabaseName = "Billing";
    private string GetDocumentId(DateTime date, string vehicleId) => $"{date:yyyy-MM-dd}_{vehicleId}";

    private readonly Lazy<Task<Container>> _lazyContainer = new(async () =>
    {
        var cosmosClient = new CosmosClient(connectionString.Replace("https", "http"), new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            LimitToEndpoint = true
        });
        await cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseName);
        var database = cosmosClient.GetDatabase(DatabaseName);
        var response = await database.CreateContainerIfNotExistsAsync(ContainerName, "/VehicleId");
        return response.Container;
    });

    // TODO cancellation token
    public async Task<DailyCharge?> GetDailyCharge(DateTime date, string vehicleId)
    {
        var container = await _lazyContainer.Value;
        var documentId = GetDocumentId(date, vehicleId);
        try
        {
            var response = await container.ReadItemAsync<DailyChargeDocument>(documentId, new PartitionKey(vehicleId));
            return response.Resource.DailyCharge;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    // TODO cancellation token
    public async Task SetDailyCharge(DailyCharge dailyCharge)
    {
        var container = await _lazyContainer.Value;
        var documentId = GetDocumentId(dailyCharge.Date, dailyCharge.VehicleId);
        var document = new DailyChargeDocument(documentId, dailyCharge.Date, dailyCharge.VehicleId, dailyCharge);
        // TODO: handle conflict if document already exists (e.g. due to multiple detections for same vehicle on same day being processed in parallel)
        await container.UpsertItemAsync(document, new PartitionKey(dailyCharge.VehicleId));
    }

    private record DailyChargeDocument(string id, DateTime Date, string VehicleId, DailyCharge DailyCharge);
}

