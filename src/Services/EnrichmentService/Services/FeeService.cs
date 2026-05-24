using System.Net.Http.Json;
using SharedTypes;

namespace EnrichmentService.Services;

public class FeeService(IHttpClientFactory httpClientFactory, ILogger<FeeService> logger)
{
    public async Task<Fee> GetFeeAsync(Guid cameraId, DateTime date)
    {
        var client = httpClientFactory.CreateClient(nameof(FeeService));
        var response = await client.GetAsync("/fee?" + $"cameraId={cameraId}&date={date:o}");
        // TODO retry logic in case of transient errors
        response.EnsureSuccessStatusCode();
        var fee = await response.Content.ReadFromJsonAsync<Fee>();
        // TODO error handling if fee is null
        return fee!;
    }
}