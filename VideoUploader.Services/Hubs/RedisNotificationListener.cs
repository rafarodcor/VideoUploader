using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace VideoUploader.Services.Hubs;

public class RedisNotificationListener : BackgroundService
{
    #region Constants

    private const string NOTIFICATION_CHANNEL = "ANALYSIS_NOTIFICATIONS";

    #endregion

    #region Properties

    private readonly ILogger<RedisNotificationListener> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<NotificationHub> _hubContext;

    #endregion

    #region Constructors

    public RedisNotificationListener(
        ILogger<RedisNotificationListener> logger,
        IConfiguration configuration,
        IHubContext<NotificationHub> hubContext)
    {
        _logger = logger;
        _hubContext = hubContext;
        var connectionString = configuration.GetConnectionString("RedisConnection");
        _redis = ConnectionMultiplexer.Connect(connectionString);
    }

    #endregion

    #region Methods

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Redis Notification Listener está rodando.");
        var subscriber = _redis.GetSubscriber();

        // Se inscreve no canal do Redis para receber as mensagens
        subscriber.Subscribe(NOTIFICATION_CHANNEL, async (channel, message) =>
        {
            _logger.LogInformation($"Notificação recebida do Redis: {(string)message}");

            // Deserializa a mensagem para um objeto anônimo
            var notification = JsonSerializer.Deserialize<JsonElement>(message);
            if (notification.TryGetProperty("AnalysisId", out var idElement) &&
                notification.TryGetProperty("Status", out var statusElement))
            {
                var analysisId = idElement.GetString();
                var status = statusElement.GetString();

                // Envia a notificação para TODOS os clientes conectados ao Hub
                await _hubContext.Clients.All.SendAsync("ReceiveAnalysisUpdate", analysisId, status, stoppingToken);
                _logger.LogInformation($"Notificação enviada via SignalR para os clientes. AnalysisId: {analysisId}");
            }
        });

        return Task.CompletedTask;
    }

    #endregion
}