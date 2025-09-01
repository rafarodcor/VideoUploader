using FFMpegCore;
using Microsoft.Extensions.Options;
using System.Drawing;
using VideoUploader.Models.Configurations;
using VideoUploader.Models.DTOs;
using ZXing.Windows.Compatibility;

namespace VideoUploader.Consumer.Services;

public class QrCodeVideoAnalysis(
    ILogger<QrCodeVideoAnalysis> logger,
    IOptions<FileStorageSettings> fileStorageSettings) : IQrCodeVideoAnalysis
{
    #region Properties

    private readonly ILogger<QrCodeVideoAnalysis> _logger = logger;
    private readonly FileStorageSettings _fileStorageSettings = fileStorageSettings.Value;

    #endregion

    #region Constructors
    #endregion

    #region Methods

    public async Task<List<QrCodeResponse>> FindQrCodeInVideoAsync(string videoPath)
    {
        // Passo 1: Obter a duração total do vídeo para saber até onde iterar
        var reader = new BarcodeReader();
        var mediaInfo = await FFProbe.AnalyseAsync(videoPath);
        var duration = mediaInfo.Duration;

        Directory.CreateDirectory(_fileStorageSettings.VideoPath);

        var results = new List<QrCodeResponse>();
        QrCodeResponse? currentAppearance = null;

        // Passo 2: Iterar pelo vídeo, extraindo um frame por segundo
        for (double seconds = 0; seconds < duration.TotalSeconds; seconds++)
        {
            string? currentFrameQrContent = null;
            var currentTime = TimeSpan.FromSeconds(seconds);
            var tempImagePath = Path.Combine(_fileStorageSettings.VideoPath, $"frame_{Guid.NewGuid()}.png");

            try
            {
                // Passo 3: Usa FFMpegCore para extrair o frame atual como uma imagem
                FFMpeg.Snapshot(videoPath, tempImagePath, new Size(640, 480), currentTime);

                // Passo 4: Tenta ler a imagem com ZXing.Net                
                using var bitmap = (Bitmap)Image.FromFile(tempImagePath);
                var result = reader.Decode(bitmap);
                if (result != null)
                {
                    currentFrameQrContent = result.Text;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Falha ao processar o frame no timestamp {currentTime} do vídeo {videoPath}. Continuando a análise.");
            }
            finally
            {
                // Garante que o arquivo temporário seja deletado
                if (File.Exists(tempImagePath))
                {
                    File.Delete(tempImagePath);
                }
            }

            // Se o QR Code atual é o mesmo que o anterior, apenas incrementamos a duração
            if (currentAppearance != null && currentAppearance.Content == currentFrameQrContent)
            {
                currentAppearance = currentAppearance with { DurationInSeconds = currentAppearance.DurationInSeconds + 1 };
            }
            else // Se for diferente (ou nulo)
            {
                // 1. Se havia um QR Code anterior sendo rastreado, sua aparição acabou. Salve-o.
                if (currentAppearance != null)
                {
                    results.Add(currentAppearance);
                }

                // 2. Se um NOVO QR Code foi encontrado neste frame, comece a rastreá-lo.
                if (currentFrameQrContent != null)
                {
                    currentAppearance = new QrCodeResponse(currentTime, currentFrameQrContent, 1);
                }
                else // Se não há QR Code neste frame, resete o rastreamento.
                {
                    currentAppearance = null;
                }
            }
        }

        // Após o loop, se ainda houver um QR Code sendo rastreado (que foi até o final do vídeo), salve-o.
        if (currentAppearance != null)
        {
            results.Add(currentAppearance);
        }

        return results;
    }
    
    #endregion
}