using Azure.Messaging.EventHubs.Producer;
using SharedTypes.Messages;

namespace SharedTypes.EventHub;

public class DetectionProducer(EventHubProducerClient producerClient)
{
    public async Task SendAsync<T>(T detectionEvent) where T : DetectionEvent
    {
        await producerClient.SendAsync([DetectionEvent.ToEventData(detectionEvent)], new SendEventOptions { PartitionKey = Guid.NewGuid().ToString() });
    }
}