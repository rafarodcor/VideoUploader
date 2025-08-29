namespace VideoUploader.Models.Models;

/// <summary>
/// Guarda informações do arquivo de vídeo para análise.
/// </summary>
public class InformationFile
{
    #region Properties

    public Guid Id { get; set; }

    public string Path { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    #endregion
}