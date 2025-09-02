using VideoUploader.Data.Repositories;
using VideoUploader.Models.Models;

namespace VideoUploader.Services.Persistence;

public class VideoAnalysisMongoService(IVideoAnalysisMongoRepository mongoRepository) : IVideoAnalysisMongoService
{
    #region Properties

    private readonly IVideoAnalysisMongoRepository _mongoRepository = mongoRepository;

    #endregion

    #region Methods

    public async Task CreateAsync(VideoAnalysis analysis)
    {
        await _mongoRepository.CreateAsync(analysis);
    }

    public async Task<VideoAnalysis?> GetAnalysisByIdAsync(Guid id)
    {
        return await _mongoRepository.GetByIdAsync(id);
    }

    public async Task<List<VideoAnalysis>> GetAllAnalysesAsync()
    {
        return await _mongoRepository.GetAllAsync();
    }

    public async Task DeleteAllAnalysesAsync()
    {
        await _mongoRepository.DeleteAllAsync();
    }

    #endregion
}