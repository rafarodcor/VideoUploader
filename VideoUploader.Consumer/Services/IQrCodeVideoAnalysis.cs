using VideoUploader.Models.DTOs;

namespace VideoUploader.Consumer.Services;

public interface IQrCodeVideoAnalysis
{
    #region Methods

    Task<List<QrCodeResponse>> FindQrCodeInVideoAsync(string videoPath);

    #endregion
}