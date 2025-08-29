using RabbitMQ.Client;

namespace VideoUploader.Consumer.MessageBus;

public class RabbitMQConnectionManager : IDisposable
{
    #region Properties

    private readonly IConnection _connection;
    private readonly IModel _channel;

    #endregion

    #region Constructors

    public RabbitMQConnectionManager(IConfiguration configuration)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQConnection:Host"],
            UserName = configuration["RabbitMQConnection:Username"],
            Password = configuration["RabbitMQConnection:Password"]
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    #endregion

    #region Methods

    public IModel GetChannel(string queueName)
    {
        _channel.QueueDeclare(
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        return _channel;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }

    #endregion
}