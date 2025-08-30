using Microsoft.Extensions.Logging;
using System.Text;
using VideoUploader.Models.DTOs;

namespace VideoUploader.Services.MessageBus;

public class UploadVideoAnalysisProducer(IMessageBus messageBus, ILogger<UploadVideoAnalysisProducer> logger) : IUploadVideoAnalysisProducer
{
    #region Constants

    private const string QUEUE_NAME = "VIDEO_ANALYSER";

    #endregion

    #region Properties

    private readonly IMessageBus _messageBus = messageBus;
    private readonly ILogger<UploadVideoAnalysisProducer> _logger = logger;

    #endregion

    #region Constructors
    #endregion

    #region Methods

    public void Publish(InformationFile informationFile)
    {
        _logger.LogInformation($"Producer > Publish > {QUEUE_NAME}");

        var message = System.Text.Json.JsonSerializer.Serialize(informationFile);
        var body = Encoding.UTF8.GetBytes(message);

        _messageBus.Publish(QUEUE_NAME, body);
    }

    #endregion
}