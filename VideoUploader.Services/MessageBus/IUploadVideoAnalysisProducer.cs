using VideoUploader.Models.Models;

namespace VideoUploader.Services.MessageBus;

public interface IUploadVideoAnalysisProducer
{
    void Publish(InformationFile informationFile);
}