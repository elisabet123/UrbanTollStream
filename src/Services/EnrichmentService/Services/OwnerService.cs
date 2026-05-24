namespace EnrichmentService.Services;

public class OwnerService
{
    public async Task<string> GetOwner(string vehicleId, DateTime date)
    {
        // TODO call OwnerService to get the owner information based on the vehicleId and date
        await Task.Delay(100);
        return $"Owner of vehicle {vehicleId}";
    }
}