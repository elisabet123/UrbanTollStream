using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs;

namespace SharedTypes.Messages;

public record DetectionEvent
{
    public static EventData ToEventData<T>(T detectionEvent) where T : DetectionEvent
    {
        return new EventData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(detectionEvent)))
        {
            ContentType = "application/json",
            Properties =
            {
                {
                    "messageType", typeof(T).FullName
                }
            }
        };
    }
}