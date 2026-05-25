namespace SharedTypes.Messages;

public record DetectionEnriched(EnrichedDetection detection) : DetectionEvent;