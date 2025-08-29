namespace VideoUploader.API.DTOs;

public class UploadResponse
{
    public Guid AnalysisId { get; set; }
    public string Message { get; set; } = string.Empty;
}