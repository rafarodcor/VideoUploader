using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace VideoUploader.Services.MessageBus;

public class MessageBus : IMessageBus
{
    #region Properties

    private readonly ConnectionFactory _connectionFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MessageBus> _logger;

    #endregion

    #region Constructors

    public MessageBus(IConfiguration configuration, ILogger<MessageBus> logger)
    {
        _configuration = configuration;

        _connectionFactory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQConnection:Host"],
            UserName = _configuration["RabbitMQConnection:Username"],
            Password = _configuration["RabbitMQConnection:Password"]
        };

        _logger = logger;
    }

    #endregion

    #region Methods

    public void Publish(string queue, byte[] message)
    {
        _logger.LogInformation("MessageBus > Publish > VideoAnalysis");

        using (var connection = _connectionFactory.CreateConnection())
        {
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(
                    queue: queue,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                channel.BasicPublish(
                    exchange: "",
                    routingKey: queue,
                    basicProperties: null,
                    body: message);
            }
        }
    }

    #endregion
}