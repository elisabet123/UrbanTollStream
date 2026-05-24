using System.Text.Json;
using Azure.Messaging.EventHubs.Consumer;

namespace SharedTypes.EventHub;

public class DetectionConsumer(EventHubConsumerClient consumerClient)
{
    private readonly Dictionary<Type, Func<object, CancellationToken, Task>> _handlers = new();
    public void On<T>(Func<T, CancellationToken, Task> handler) where T : class
    {
        _handlers[typeof(T)] = (o, ct) => handler((T)o, ct);
    }

    public async Task Start(CancellationToken cancellationToken)
    {
        var events = consumerClient.ReadEventsAsync(cancellationToken: cancellationToken);
        await foreach (var e in events)
        {
            try
            {
                var eventType = e.Data.Properties["messageType"] as string;
                if (eventType == null)
                {
                    Console.WriteLine("Event does not contain MessageType property.");
                    continue;
                }

                var type = Type.GetType(eventType);
                if (type == null)
                {
                    Console.WriteLine($"Unknown event type: {eventType}");
                    continue;
                }

                if (_handlers.TryGetValue(type, out var handler))
                {
                    var message = e.Data.EventBody.ToString();
                    var deserializedMessage = JsonSerializer.Deserialize(message, type);
                    if (deserializedMessage != null)
                    {
                        await handler(deserializedMessage, cancellationToken);
                    }
                    else
                    {
                        Console.WriteLine($"Failed to deserialize message of type {eventType}");
                    }
                }
                else
                {
                    Console.WriteLine($"No handler registered for event type: {eventType}");
                }
            } catch (Exception ex)
            {
                // Handle exceptions that occur during event processing
                Console.WriteLine($"Error processing event: {ex.Message}");
            }
        }
    }
}