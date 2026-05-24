using System.Net.Http.Json;
using SharedTypes;

namespace EnrichmentService.Services;

public class OwnerService(IHttpClientFactory httpClientFactory, ILogger<OwnerService> logger)
{
    public async Task<string> GetOwner(string vehicleId, DateTime date)
    {
        var client = httpClientFactory.CreateClient(nameof(OwnerService));
        var response = await client.GetAsync("/owner?" + $"vehicleId={vehicleId}&date={date:o}");
        // TODO retry logic in case of transient errors
        response.EnsureSuccessStatusCode();
        var owner = await response.Content.ReadFromJsonAsync<Owner>();
        // TODO error handling if owner is null
        return $"{owner!.firstName} {owner.lastName}";
    }
}