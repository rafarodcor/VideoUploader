namespace VideoUploader.Models.Models;

/// <summary>
/// Armazena os dados do QR Code encontrado no vídeo.
/// </summary>
public class QrCodeData
{
    #region Properties

    /// <summary>
    /// Identificador único do QR Code
    /// </summary>    
    public Guid Id { get; set; }

    /// <summary>
    /// Id do VideoAnalysis ao qual este QR Code pertence
    /// </summary>
    public Guid VideoAnalysisId { get; set; }

    /// <summary>
    /// O conteúdo decodificado do QR Code
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// O momento no vídeo em que o QR Code foi encontrado (em segundos)
    /// </summary>
    public TimeSpan Timestamp { get; set; }

    #endregion
}