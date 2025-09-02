using VideoUploader.Models.Models;

namespace VideoUploader.Data.Repositories;

public interface IVideoAnalysisMongoRepository
{
    #region Methods

    Task CreateAsync(VideoAnalysis analysis);
    Task UpdateAsync(VideoAnalysis analysis);
    Task<VideoAnalysis?> GetByIdAsync(Guid id);
    Task<List<VideoAnalysis>> GetAllAsync();
    Task DeleteAllAsync();

    #endregion
}