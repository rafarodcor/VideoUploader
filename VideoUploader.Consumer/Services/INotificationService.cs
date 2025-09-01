namespace VideoUploader.Consumer.Services;

public interface INotificationService
{
    #region Methods

    Task NotifyAnalysisUpdate(Guid analysisId, string status);

    #endregion
}