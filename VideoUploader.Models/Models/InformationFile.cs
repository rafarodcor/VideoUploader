namespace VideoUploader.Models.Models;

/// <summary>
/// Guarda informações do arquivo de vídeo para análise.
/// </summary>
public record InformationFile(Guid Id, string FileName, string Path);