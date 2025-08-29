using System.ComponentModel.DataAnnotations;

namespace VideoUploader.Models.Models;

public class Enums
{
    /// <summary>
    /// Enum para representar o status do processamento do vídeo.
    /// </summary>
    public enum ProcessingStatus
    {
        [Display(Name = "Na fila")]
        InQueue,
        [Display(Name = "Processando")]
        Processing,
        [Display(Name = "Concluído")]
        Completed,
        [Display(Name = "Falhou")]
        Failed
    }
}