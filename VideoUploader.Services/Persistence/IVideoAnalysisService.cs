using VideoUploader.Models.Models;

namespace VideoUploader.Services.Persistence;

public interface IVideoAnalysisService
{
    #region Methods

    Task<IEnumerable<QrCodeData>> GetQrCodeDataAsync(Guid id);
    Task<VideoAnalysis> GetAnalysisStatus(Guid id);
    Task UploadVideoAsync(InformationFile informationFile);
    Task SaveAnalysisStatus(VideoAnalysis videoAnalysis);
    Task UpdateAnalysisStatus(VideoAnalysis videoAnalysis);
    Task SaveListQrCodeData(List<QrCodeData> listQrCodeData);

    #endregion
}