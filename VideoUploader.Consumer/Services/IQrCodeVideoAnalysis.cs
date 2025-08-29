namespace VideoUploader.Consumer.Services;

public interface IQrCodeVideoAnalysis
{
    #region Methods

    Task<List<(TimeSpan? Timestamp, string? QrCodeContent)>> FindQrCodeInVideoAsync(string videoPath);

    #endregion
}