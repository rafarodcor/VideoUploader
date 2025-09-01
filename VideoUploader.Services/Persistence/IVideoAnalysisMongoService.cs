using VideoUploader.Models.Models;

namespace VideoUploader.Services.Persistence;

public interface IVideoAnalysisMongoService
{
    #region Methods

    Task CreateAsync(VideoAnalysis analysis);
    Task<VideoAnalysis?> GetAnalysisByIdAsync(Guid id);

    #endregion
}