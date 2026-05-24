
namespace SharedTypes.Messages;

public class DetectionCreated(Detection detection) : DetectionEvent<DetectionCreated>(detection);