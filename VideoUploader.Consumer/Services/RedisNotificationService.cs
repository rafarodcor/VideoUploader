using StackExchange.Redis;
using System.Text.Json;

namespace VideoUploader.Consumer.Services;

public class RedisNotificationService : INotificationService
{
    #region Constants

    private const string NOTIFICATION_CHANNEL = "ANALYSIS_NOTIFICATIONS";

    #endregion

    #region Properties

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisNotificationService> _logger;

    #endregion

    #region Constructors

    public RedisNotificationService(IConfiguration configuration, ILogger<RedisNotificationService> logger)
    {
        var connectionString = configuration.GetConnectionString("RedisConnection");
        _redis = ConnectionMultiplexer.Connect(connectionString);
        _logger = logger;
    }

    #endregion

    #region Methods

    public async Task NotifyAnalysisUpdate(Guid analysisId, string status)
    {
        var subscriber = _redis.GetSubscriber();

        var notificationPayload = new
        {
            AnalysisId = analysisId,
            Status = status
        };
        var message = JsonSerializer.Serialize(notificationPayload);

        _logger.LogInformation($"Publicando notificação de status para o Redis no canal '{NOTIFICATION_CHANNEL}'. AnalysisId: {analysisId}");

        // Publica a mensagem no canal especificado. O 'FireAndForget' é ótimo para notificações.
        await subscriber.PublishAsync(NOTIFICATION_CHANNEL, message, CommandFlags.FireAndForget);
    }

    #endregion
}