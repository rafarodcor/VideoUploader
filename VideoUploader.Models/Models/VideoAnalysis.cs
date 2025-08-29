namespace VideoUploader.Models.Models;

/// <summary>
/// Entidade principal que representa uma análise de vídeo.
/// </summary>
public class VideoAnalysis
{
    #region Properties

    /// <summary>
    /// Identificador único para cada solicitação de análise.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Nome original do arquivo de vídeo enviado.
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Status atual do processamento do vídeo.
    /// </summary>
    public Enums.ProcessingStatus Status { get; set; }

    /// <summary>
    /// Data e hora em que a análise foi solicitada.
    /// </summary>
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    #endregion
}