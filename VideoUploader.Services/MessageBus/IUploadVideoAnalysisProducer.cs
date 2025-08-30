using VideoUploader.Models.DTOs;

namespace VideoUploader.Services.MessageBus;

public interface IUploadVideoAnalysisProducer
{
    void Publish(InformationFile informationFile);
}