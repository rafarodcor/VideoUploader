using VideoUploader.Models.Models;

namespace VideoUploader.Data.Repositories;

public interface IVideoAnalysisRepository
{
    #region Methods

    Task<IEnumerable<QrCodeData>> GetQrCodeDataAsync(Guid id);
    Task<VideoAnalysis> GetAnalysisStatus(Guid id);
    Task SaveAnalysisStatus(VideoAnalysis videoAnalysis);
    Task UpdateAnalysisStatus(VideoAnalysis videoAnalysis);    
    Task SaveListQrCodeData(List<QrCodeData> listQrCodeData);
    Task<IEnumerable<VideoAnalysis>> GetAllAsync();
    Task DeleteAllAsync();

    #endregion
}