namespace VideoUploader.Models.DTOs;

/// <summary>
/// Guarda informações do arquivo de vídeo para análise.
/// </summary>
public record InformationFile(Guid Id, string FileName, string Path);