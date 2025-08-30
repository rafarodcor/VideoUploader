using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using VideoUploader.Consumer.Services;
using VideoUploader.Data.Repositories;
using VideoUploader.Models.DTOs;
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

                _logger.LogInformation("Iniciando processamento para a análise: {AnalysisId}", informationFile.Id);

                var videoRepository = scope.ServiceProvider.GetRequiredService<IVideoAnalysisRepository>();
                var qrCodeAnalysisService = scope.ServiceProvider.GetRequiredService<IQrCodeVideoAnalysis>();

                VideoAnalysis? videoAnalysis = null;

                try
                {
                    videoAnalysis = await videoRepository.GetAnalysisStatus(informationFile.Id);

                    if (videoAnalysis == null)
                    {
                        _logger.LogError("Análise de vídeo com ID {AnalysisId} não encontrada no banco de dados.", informationFile.Id);
                        _channel.BasicAck(eventArgs.DeliveryTag, false); // Remove a mensagem "morta"
                        return;
                    }

                    // 1. Atualiza o status para "Processando"
                    videoAnalysis.Status = Enums.ProcessingStatus.Processing;
                    await videoRepository.UpdateAnalysisStatus(videoAnalysis);

                    // 2. Faz a análise do vídeo
                    var listTimestamps = await qrCodeAnalysisService.FindQrCodeInVideoAsync(videoPath);

                    // 3. Salva os resultados
                    if (listTimestamps.Count > 0)
                    {
                        var listQrCodeData = listTimestamps.Select(t => new QrCodeData
                        {
                            VideoAnalysisId = informationFile.Id,
                            Timestamp = t.Timestamp,
                            Content = t.Content,
                            DurationInSeconds = t.DurationInSeconds
                        }).ToList();

                        await videoRepository.SaveListQrCodeData(listQrCodeData);
                    }

                    // 4. Atualiza o status para "Concluído"
                    videoAnalysis.Status = Enums.ProcessingStatus.Completed;
                    await videoRepository.UpdateAnalysisStatus(videoAnalysis);

                    _logger.LogInformation("Processamento concluído com sucesso para a análise: {AnalysisId}", informationFile.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha crítica ao processar a análise: {AnalysisId}", informationFile.Id);

                    if (videoAnalysis != null)
                    {
                        // 5. Atualiza o status para "Falhou"
                        videoAnalysis.Status = Enums.ProcessingStatus.Failed;
                        await videoRepository.UpdateAnalysisStatus(videoAnalysis);
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