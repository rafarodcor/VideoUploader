using Microsoft.AspNetCore.Mvc;
using VideoUploader.API.DTOs;
using VideoUploader.Models.Helpers;
using VideoUploader.Models.Models;
using VideoUploader.Services.Persistence;

namespace VideoUploader.API.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class VideoController(
    ILogger<VideoController> logger,
    IVideoAnalysisService videoAnalysisService) : ControllerBase
{
    #region Constants

    private static readonly List<string> ALLOWED_EXTENSIONS = [".mp4", ".avi", ".mkv"];

    #endregion

    #region Constructors
    #endregion

    #region Properties

    private readonly ILogger<VideoController> _logger = logger;
    private readonly IVideoAnalysisService _videoAnalysisService = videoAnalysisService;

    #endregion

    #region Methods

    [HttpPost("/upload-video")]
    [ProducesResponseType(typeof(List<UploadResponse>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadVideoAsync(List<IFormFile> videoFiles)
    {
        // 1. Validar o arquivo recebido
        if (videoFiles == null || videoFiles.Count == 0)
        {
            return BadRequest("Nenhum arquivo de vídeo foi enviado.");
        }

        var responses = new List<UploadResponse>();

        foreach (var videoFile in videoFiles)
        {
            var extension = Path.GetExtension(videoFile.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !ALLOWED_EXTENSIONS.Contains(extension))
            {
                return BadRequest("Formato de arquivo inválido. Apenas .mp4, .avi e .mkv são permitidos.");
            }

            // 2. Criar a entidade de análise
            var analysis = new VideoAnalysis
            {
                Id = Guid.NewGuid(),
                OriginalFileName = videoFile.FileName,
                Status = Enums.ProcessingStatus.InQueue,
                SubmittedAt = DateTime.UtcNow
            };

            // 3. Salvar o arquivo em um local temporário/persistente
            var videoPath = Path.Combine("Files", $"{analysis.Id}{extension}");
            try
            {
                await using (var stream = new FileStream(videoPath, FileMode.Create))
                {
                    await videoFile.CopyToAsync(stream);
                }

                _logger.LogInformation($"Vídeo {analysis.OriginalFileName} em {videoPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Falha ao salvar o vídeo {analysis.OriginalFileName}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao salvar o arquivo de vídeo.");
            }

            // 4. Salvar o status inicial da análise
            try
            {
                await _videoAnalysisService.SaveAnalysisStatus(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar o Status da análise no banco de dados.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao salvar o estado da análise.");
            }

            // 5. Publicar a mensagem na fila para processamento assíncrono        
            await _videoAnalysisService.UploadVideoAsync(new InformationFile { Id = analysis.Id, FileName = analysis.OriginalFileName, Path = videoPath });

            _logger.LogInformation($"Análise {analysis.Id} enfileirada para processamento.");

            // 6. Retornar a resposta para o cliente
            responses.Add(new UploadResponse
            {
                AnalysisId = analysis.Id,
                Message = "Vídeo recebido e enfileirado para análise."
            });
        }

        // 7. Retorna 202 Accepted
        return Accepted(responses);
    }

    [HttpGet("/get-status/{id}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalysisStatus(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest("O ID fornecido é inválido.");
        }

        try
        {
            var analysis = await _videoAnalysisService.GetAnalysisStatus(id);
            if (analysis == null)
            {
                return NotFound($"Análise com ID {id} não encontrada.");
            }

            return Ok($"Status da análise: {analysis.Status.GetDisplayNameEnum()}");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("/get-list-qrcode")]
    [ProducesResponseType(typeof(List<QrCodeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQrCodeDataAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest("O ID fornecido é inválido.");
        }

        try
        {
            var listQrCodeData = await _videoAnalysisService.GetQrCodeDataAsync(id);
            if (listQrCodeData == null || !listQrCodeData.Any())
            {
                return NotFound($"Nenhum dado de QR Code encontrado para a análise com ID {id}.");
            }
            else
            {
                return Ok(listQrCodeData.Select(qr => new QrCodeResponse
                {
                    Timestamp = qr.Timestamp,
                    Content = qr.Content
                }).ToList());
            }
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    #endregion
}
