using FFMpegCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using VideoUploader.Consumer.Services;
using VideoUploader.Data.Repositories;
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
        _logger.LogInformation($"Consumer > ExecuteAsync > {QUEUE_NAME} > ExecuteAsync");

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (sender, eventArgs) =>
        {
            var modelBytes = eventArgs.Body.ToArray();
            var modelJson = Encoding.UTF8.GetString(modelBytes);
            var informationFile = JsonSerializer.Deserialize<InformationFile>(modelJson);

            await PostAsync(informationFile!);

            _channel.BasicAck(eventArgs.DeliveryTag, false);
        };

        _channel.BasicConsume(QUEUE_NAME, false, consumer);
        return Task.CompletedTask;
    }

    public async Task PostAsync(InformationFile informationFile)
    {
        _logger.LogInformation($"Consumer > ExecuteAsync > {QUEUE_NAME} > PostAsync");

        var videoPath = $@"..\VideoUploader.API\{informationFile.Path}";

        try
        {
            if (informationFile != null)
            {
                using var scope = _serviceProvider.CreateScope();
                var videoRepository = scope.ServiceProvider.GetRequiredService<IVideoAnalysisRepository>();

                //Recupera o vídeo pelo Id pra atualizar o status
                var videoAnalysis = await videoRepository.GetAnalysisStatus(informationFile.Id);

                if (videoAnalysis != null)
                {
                    videoAnalysis.Status = Enums.ProcessingStatus.Processing;
                    videoAnalysis.SubmittedAt = DateTime.UtcNow;
                    await videoRepository.UpdateAnalysisStatus(videoAnalysis);                                        
                                        
                    //Manipula o vídeo para encontrar os qrCodes                    
                    var listTimestamps = await _qrCodeVideoAnalysis.FindQrCodeInVideoAsync(videoPath);

                    _logger.LogInformation($"Consumer > ExecuteAsync > {QUEUE_NAME} > FindQrCodeInVideoAsync");

                    if (listTimestamps.Count != 0)
                    {
                        List<QrCodeData> listQrCodeData = [];

                        foreach (var (Timestamp, QrCodeContent) in listTimestamps)
                        {
                            listQrCodeData.Add(new QrCodeData
                            {
                                VideoAnalysisId = informationFile.Id,
                                Timestamp = (TimeSpan)Timestamp,
                                Content = QrCodeContent
                            });
                        }

                        await videoRepository.SaveListQrCodeData(listQrCodeData);

                        _logger.LogInformation($"Consumer > ExecuteAsync > {QUEUE_NAME} > SaveListQrCodeData");
                    }                    
                    
                    videoAnalysis.Status = Enums.ProcessingStatus.Completed;
                    videoAnalysis.SubmittedAt = DateTime.UtcNow;
                    await videoRepository.UpdateAnalysisStatus(videoAnalysis);

                    _logger.LogInformation($"Consumer > ExecuteAsync > {QUEUE_NAME} > UpdateAnalysisStatus");
                }                
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocorreu um erro ao processar o vídeo: {Path}", videoPath);
            throw;
        }
        finally
        {
            // Limpeza: garante que o arquivo temporário seja deletado
            if (File.Exists(videoPath))
            {
                _logger.LogInformation("Deletando arquivo temporário: {Path}", videoPath);
                File.Delete(videoPath);
            }
        }
    }    

    #endregion
}