using Azure.Messaging.EventHubs.Producer;
using SharedTypes.Messages;

namespace SharedTypes.EventHub;

public class DetectionProducer(EventHubProducerClient producerClient)
{
    public async Task SendAsync<T>(DetectionEvent<T> detectionEvent)
    {
        await producerClient.SendAsync([detectionEvent.ToEventData()]);
    }
}