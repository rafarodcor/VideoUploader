using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using VideoUploader.Consumer.Services;
using VideoUploader.Data.Repositories;
using VideoUploader.Models.DTOs;
using VideoUploader.Models.Helpers;
using VideoUploader.Models.Models;

namespace VideoUploader.Consumer.MessageBus;

public class UploadVideoAnalysisConsumer : BackgroundService
{
    #region Constants

    private const string QUEUE_NAME = "VIDEO_ANALYSER";

    #endregion  

    #region Properties

    private readonly RabbitMQConnectionManager _connectionManager;
    private readonly IModel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UploadVideoAnalysisConsumer> _logger;
    private readonly IQrCodeVideoAnalysis _qrCodeVideoAnalysis;

    #endregion

    #region Constructors

    public UploadVideoAnalysisConsumer(
        RabbitMQConnectionManager connectionManager,
        IServiceProvider servicesProvider,
        ILogger<UploadVideoAnalysisConsumer> logger,
        IQrCodeVideoAnalysis qrCodeVideoAnalysis)
    {
        _connectionManager = connectionManager;
        _serviceProvider = servicesProvider;
        _logger = logger;
        _qrCodeVideoAnalysis = qrCodeVideoAnalysis;
        _channel = _connectionManager.GetChannel(QUEUE_NAME);
    }

    #endregion

    #region Methods

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (sender, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();
            var messageJson = Encoding.UTF8.GetString(body);
            var informationFile = JsonSerializer.Deserialize<InformationFile>(messageJson);

            if (informationFile == null)
            {
                _logger.LogWarning("Mensagem recebida inválida ou vazia.");
                _channel.BasicAck(eventArgs.DeliveryTag, false);
                return;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                var videoPath = informationFile.Path;

                _logger.LogInformation($"Iniciando processamento para a análise: {informationFile.Id}");

                var videoRepository = scope.ServiceProvider.GetRequiredService<IVideoAnalysisRepository>();
                var mongoRepository = scope.ServiceProvider.GetRequiredService<IVideoAnalysisMongoRepository>();
                var qrCodeAnalysisService = scope.ServiceProvider.GetRequiredService<IQrCodeVideoAnalysis>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                VideoAnalysis? videoAnalysis = null;

                try
                {
                    videoAnalysis = await videoRepository.GetAnalysisStatus(informationFile.Id);

                    if (videoAnalysis == null)
                    {
                        _logger.LogError($"Análise de vídeo com ID {informationFile.Id} não encontrada no banco de dados.");
                        _channel.BasicAck(eventArgs.DeliveryTag, false);
                        return;
                    }

                    // Envia a notificação de vídeo 'Na Fila'
                    await notificationService.NotifyAnalysisUpdate(videoAnalysis.Id, videoAnalysis.Status.GetDisplayNameEnum());

                    // 1. Atualiza o status para "Processando"
                    videoAnalysis.Status = Enums.ProcessingStatus.Processing;
                    await videoRepository.UpdateAnalysisStatus(videoAnalysis);
                    await mongoRepository.UpdateAsync(videoAnalysis);
                                        
                    await notificationService.NotifyAnalysisUpdate(videoAnalysis.Id, videoAnalysis.Status.GetDisplayNameEnum());

                    // 2. Faz a análise do vídeo
                    var listQrCodeResponses = await qrCodeAnalysisService.FindQrCodeInVideoAsync(videoPath);

                    // 3. Salva os resultados
                    if (listQrCodeResponses.Count > 0)
                    {
                        var listQrCodeData = listQrCodeResponses.Select(t => new QrCodeData
                        {
                            VideoAnalysisId = informationFile.Id,
                            Timestamp = t.Timestamp,
                            Content = t.Content,
                            DurationInSeconds = t.DurationInSeconds
                        }).ToList();

                        await videoRepository.SaveListQrCodeData(listQrCodeData);

                        // Adiciona os QrCodes à lista aninhada do objeto principal para o Mongo
                        videoAnalysis.QrCodes.AddRange(listQrCodeData);
                    }

                    // 4. Atualiza o status para "Concluído"
                    videoAnalysis.Status = Enums.ProcessingStatus.Completed;
                    await videoRepository.UpdateAnalysisStatus(videoAnalysis);
                    await mongoRepository.UpdateAsync(videoAnalysis);

                    await notificationService.NotifyAnalysisUpdate(videoAnalysis.Id, videoAnalysis.Status.GetDisplayNameEnum());

                    _logger.LogInformation($"Processamento concluído com sucesso para a análise: {informationFile.Id}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Falha crítica ao processar a análise: {informationFile.Id}");

                    if (videoAnalysis != null)
                    {
                        // 5. Atualiza o status para "Falhou"
                        videoAnalysis.Status = Enums.ProcessingStatus.Failed;
                        await videoRepository.UpdateAnalysisStatus(videoAnalysis);
                        await mongoRepository.UpdateAsync(videoAnalysis);

                        await notificationService.NotifyAnalysisUpdate(videoAnalysis.Id, videoAnalysis.Status.GetDisplayNameEnum());
                    }
                }
                finally
                {
                    // Limpeza do arquivo físico
                    if (File.Exists(videoPath))
                    {
                        File.Delete(videoPath);
                    }

                    // 6. Confirma a mensagem (ACK) em TODOS os casos (sucesso ou falha tratada)                    
                    _channel.BasicAck(eventArgs.DeliveryTag, false);
                }
            }
        };

        _channel.BasicConsume(QUEUE_NAME, false, consumer);
        return Task.CompletedTask;
    }

    #endregion
}