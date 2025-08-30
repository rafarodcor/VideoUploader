namespace VideoUploader.API.DTOs;

public record QrCodeResponse(TimeSpan? Timestamp, string? Content);