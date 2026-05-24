namespace SharedTypes.Messages;

public class DetectionEnriched(Detection detection, EnrichmentResult enrichmentResult) : DetectionEvent<DetectionEnriched>(detection)
{
    public EnrichmentResult EnrichmentResult { get; } = enrichmentResult;
}