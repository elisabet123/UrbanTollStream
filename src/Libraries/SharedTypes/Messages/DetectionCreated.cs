using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs;

namespace SharedTypes.Messages;

public class DetectionCreated(Detection detection)
{
    public Detection Detection { get; } = detection;
    public EventData ToEventData()
    {
        return new EventData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this)))
        {
            ContentType = "application/json",
            Properties =
            {
                {
                    "messageType", typeof(DetectionCreated).FullName
                }
            }
        };
    }
}