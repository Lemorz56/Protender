using NATS.Client;

namespace Protender;

public class NatsPublisher
{
    private readonly IConnection _connection;

    public NatsPublisher(IConnection connection)
    {
        _connection = connection;
    }

    public void PublishMessage(string subject, byte[] bytes, int count)
    {
        for (var i = 0; i < count; i++) _connection.Publish(subject, bytes);
    }
}