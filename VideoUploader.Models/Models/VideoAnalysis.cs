using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

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
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    /// <summary>
    /// Nome original do arquivo de vídeo enviado.
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Extensão do arquivo de vídeo (ex: .mp4, .avi, .mkv).
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// Status atual do processamento do vídeo.
    /// </summary>
    public Enums.ProcessingStatus Status { get; set; }

    /// <summary>
    /// Data e hora em que a análise foi solicitada.
    /// </summary>
    public DateTime SubmittedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Lista dos dados de QR Code encontrados, aninhados neste documento.
    /// Uso exclusivo do MongoDB
    /// </summary>
    public List<QrCodeData> QrCodes { get; set; } = [];

    #endregion
}