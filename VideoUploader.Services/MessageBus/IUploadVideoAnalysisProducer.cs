using Microsoft.AspNetCore.Http;
using VideoUploader.Models.Models;

namespace VideoUploader.Services.MessageBus;

public interface IUploadVideoAnalysisProducer
{
    void Publish(Guid id, IFormFile videoFile);

    void Publish(InformationFile informationFile);
}