using SharedTypes;

namespace EnrichmentService.Services;

public class FeeService
{
    public async Task<Fee> GetFeeAsync(string vehicleId, DateTime date)
    {
        // Placeholder implementation. In a real implementation, this would query a database or another service to get the fee information based on the vehicleId and date.
        await Task.Delay(100); // Simulate async work
        Console.WriteLine($"Enriched with fee for vehicle {vehicleId} on date {date}");
        // TODO call FeeService to get the fee information based on the vehicleId and date
        return new Fee(20, "v1.0", new[] { "Rule1", "Rule2" });
    }
}