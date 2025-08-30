namespace VideoUploader.Models.DTOs;

public record QrCodeResponse(TimeSpan Timestamp, string? Content, int DurationInSeconds)
{
    public override string ToString()
    {
        string formattedTimestamp = Timestamp.ToString(@"hh\:mm\:ss");
        return $"QrCode encontrado no tempo {formattedTimestamp} com o conteúdo \"{Content}\". Ficou visível na tela por {DurationInSeconds} segundo(s).";
    }
}