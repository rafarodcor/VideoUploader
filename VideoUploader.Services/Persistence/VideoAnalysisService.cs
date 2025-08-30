using VideoUploader.Data.Repositories;
using VideoUploader.Models.DTOs;
using VideoUploader.Models.Models;
using VideoUploader.Services.MessageBus;

namespace VideoUploader.Services.Persistence;

public class VideoAnalysisService(
    IVideoAnalysisRepository videoAnalysisRepository,
    IUploadVideoAnalysisProducer uploadVideoAnalysisProducer) : IVideoAnalysisService
{
    #region Properties

    private readonly IVideoAnalysisRepository _videoAnalysisRepository = videoAnalysisRepository;
    private readonly IUploadVideoAnalysisProducer _uploadVideoAnalysisProducer = uploadVideoAnalysisProducer;

    #endregion

    #region Constructor
    #endregion

    #region Methods

    public async Task<VideoAnalysis> GetAnalysisStatus(Guid id)
    {
        return await _videoAnalysisRepository.GetAnalysisStatus(id);
    }

    public async Task<IEnumerable<QrCodeData>> GetQrCodeDataAsync(Guid id)
    {
        return await _videoAnalysisRepository.GetQrCodeDataAsync(id);
    }

    public async Task SaveAnalysisStatus(VideoAnalysis videoAnalysis)
    {
        await _videoAnalysisRepository.SaveAnalysisStatus(videoAnalysis);
    }

    public async Task UpdateAnalysisStatus(VideoAnalysis videoAnalysis)
    {
        await _videoAnalysisRepository.UpdateAnalysisStatus(videoAnalysis);
    }

    public async Task SaveListQrCodeData(List<QrCodeData> listQrCodeData)
    {
        await _videoAnalysisRepository.SaveListQrCodeData(listQrCodeData);
    }

    public void UploadVideo(InformationFile informationFile)
    {
        _uploadVideoAnalysisProducer.Publish(informationFile);
    }

    #endregion
}