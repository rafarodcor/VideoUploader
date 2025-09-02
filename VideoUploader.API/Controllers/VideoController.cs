using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using VideoUploader.Models.Configurations;
using VideoUploader.Models.DTOs;
using VideoUploader.Models.Helpers;
using VideoUploader.Models.Models;
using VideoUploader.Services.Persistence;

namespace VideoUploader.API.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class VideoController(
    ILogger<VideoController> logger,
    IVideoAnalysisService videoAnalysisService,
    IVideoAnalysisMongoService videoAnalysisMongoService,
    IOptions<FileStorageSettings> fileStorageSettings,
    IOptions<UploadSettings> uploadSettings) : ControllerBase
{
    #region Constructors
    #endregion

    #region Properties

    private readonly ILogger<VideoController> _logger = logger;
    private readonly IVideoAnalysisService _videoAnalysisService = videoAnalysisService;
    private readonly IVideoAnalysisMongoService _videoAnalysisMongoService = videoAnalysisMongoService;
    private readonly FileStorageSettings _fileStorageSettings = fileStorageSettings.Value;
    private readonly UploadSettings _uploadSettings = uploadSettings.Value;

    #endregion

    #region Methods

    [EnableRateLimiting("fixed")]
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
            if (!IsFileExtensionValid(videoFile))
            {
                var allowedExtensionsString = string.Join(", ", _uploadSettings.AllowedExtensions);
                return BadRequest($"Formato de arquivo inválido para '{videoFile.FileName}'. Apenas {allowedExtensionsString} são permitidos.");
            }

            // 2. Criar a entidade de análise
            var analysis = new VideoAnalysis
            {
                Id = Guid.NewGuid(),
                OriginalFileName = videoFile.FileName,
                Extension = Path.GetExtension(videoFile.FileName).ToLowerInvariant(),
                Status = Enums.ProcessingStatus.InQueue,
                SubmittedAt = DateTime.Now
            };

            // 3. Salvar o arquivo em um local temporário/persistente
            var videoPath = Path.Combine(_fileStorageSettings.VideoPath, $"{analysis.Id}{analysis.Extension}");

            try
            {
                Directory.CreateDirectory(_fileStorageSettings.VideoPath);

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
                await _videoAnalysisMongoService.CreateAsync(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar o Status da análise no banco de dados.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao salvar o estado da análise.");
            }

            // 5. Publicar a mensagem na fila para processamento assíncrono        
            _videoAnalysisService.UploadVideo(new InformationFile(analysis.Id, analysis.OriginalFileName, videoPath));

            _logger.LogInformation($"Análise {analysis.Id} enfileirada para processamento.");

            // 6. Retornar a resposta para o cliente
            responses.Add(new UploadResponse(analysis.Id, "Vídeo recebido e enfileirado para análise."));
        }

        // 7. Retorna 202 Accepted
        return Accepted(responses);
    }

    #region SqlServer

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
            _logger.LogError(ex, $"Erro ao buscar status para a análise ID {id}");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado ao consultar os dados de QR Code.");
        }
    }

    [HttpGet("/get-list-qrcode/{id}")]
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
                return Ok(listQrCodeData
                    .Select(qr => new QrCodeResponse(qr.Timestamp, qr.Content, qr.DurationInSeconds))
                    .Select(response => response.ToString())
                    .ToList());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao buscar dados de QR Code para a análise ID {id}");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado ao consultar os dados de QR Code.");
        }
    }

    [Tags("Video - Bonus Endpoints")]
    [HttpGet("/get-all-analyses")]
    [ProducesResponseType(typeof(IEnumerable<VideoAnalysis>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAnalyses()
    {
        var analyses = await _videoAnalysisService.GetAllAnalysesAsync();
        return Ok(analyses);
    }

    [Tags("Video - Bonus Endpoints")]
    [HttpDelete("/delete-all-data")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAllData()
    {
        try
        {
            _logger.LogWarning("Iniciando a exclusão de todos os dados de análise.");

            // Chama a deleção em ambos os bancos de dados
            await _videoAnalysisService.DeleteAllAnalysesAsync();
            await _videoAnalysisMongoService.DeleteAllAnalysesAsync();

            _logger.LogWarning("Todos os dados de análise foram excluídos com sucesso.");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha crítica ao tentar deletar todos os dados.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Ocorreu um erro ao deletar os dados.");
        }
    }

    #endregion

    #region MongoDB

    [Tags("Video - MongoDB")]
    [HttpGet("/mongo/get-status/{id}")]
    [ProducesResponseType(typeof(VideoAnalysis), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalysisStatusFromMongo(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest("O ID fornecido é inválido.");
        }

        var analysis = await _videoAnalysisMongoService.GetAnalysisByIdAsync(id);

        if (analysis == null)
        {
            return NotFound($"Análise com ID {id} não encontrada no MongoDB.");
        }

        return Ok($"Status da análise: {analysis.Status.GetDisplayNameEnum()}");
    }

    [Tags("Video - MongoDB")]
    [HttpGet("/mongo/get-list-qrcode/{id}")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQrCodeDataFromMongo(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest("O ID fornecido é inválido.");
        }

        var analysis = await _videoAnalysisMongoService.GetAnalysisByIdAsync(id);

        if (analysis == null || !analysis.QrCodes.Any())
        {
            return NotFound($"Nenhum dado de QR Code encontrado para a análise com ID {id} no MongoDB.");
        }

        return Ok(analysis.QrCodes
            .Select(qr => new QrCodeResponse(qr.Timestamp, qr.Content, qr.DurationInSeconds))
            .Select(response => response.ToString())
            .ToList());
    }

    [Tags("Video - Bonus Endpoints")]
    [HttpGet("/mongo/get-all-analyses")]
    [ProducesResponseType(typeof(List<VideoAnalysis>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllAnalysesFromMongo()
    {
        var analyses = await _videoAnalysisMongoService.GetAllAnalysesAsync();
        return Ok(analyses);
    }

    #endregion

    #region Private methods

    private bool IsFileExtensionValid(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !_uploadSettings.AllowedExtensions.Contains(extension))
        {
            return false;
        }
        return true;
    }

    #endregion

    #endregion
}