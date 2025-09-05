using FFMpegCore;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using VideoUploader.Models.Configurations;
using VideoUploader.Models.DTOs;
using ZXing;

namespace VideoUploader.Consumer.Services;

public class QrCodeVideoAnalysis(
    ILogger<QrCodeVideoAnalysis> logger,
    IOptions<FileStorageSettings> fileStorageSettings) : IQrCodeVideoAnalysis
{
    #region Properties

    private readonly ILogger<QrCodeVideoAnalysis> _logger = logger;
    private readonly FileStorageSettings _fileStorageSettings = fileStorageSettings.Value;

    #endregion

    #region Methodos

    public async Task<List<QrCodeResponse>> FindQrCodeInVideoAsync(string videoPath)
    {
        var reader = new ZXing.ImageSharp.BarcodeReader<Rgba32>
        {
            Options = new ZXing.Common.DecodingOptions
            {
                PossibleFormats = [BarcodeFormat.QR_CODE],
                TryHarder = true
            }
        };

        var mediaInfo = await FFProbe.AnalyseAsync(videoPath);
        var duration = mediaInfo.Duration;

        Directory.CreateDirectory(_fileStorageSettings.VideoPath);

        var results = new List<QrCodeResponse>();
        QrCodeResponse? currentAppearance = null;

        for (double seconds = 0; seconds < duration.TotalSeconds; seconds++)
        {
            string? currentFrameQrContent = null;
            var currentTime = TimeSpan.FromSeconds(seconds);
            var tempImagePath = Path.Combine(_fileStorageSettings.VideoPath, $"frame_{Guid.NewGuid()}.png");

            try
            {
                // Extrair o frame com FFMpegCore
                FFMpeg.Snapshot(videoPath, tempImagePath, new System.Drawing.Size(640, 480), currentTime);

                // Carregar com ImageSharp
                using var image = Image.Load<Rgba32>(tempImagePath);

                // Ler o QRCode
                var result = reader.Decode(image);
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
                if (File.Exists(tempImagePath))
                {
                    File.Delete(tempImagePath);
                }
            }

            if (currentAppearance != null && currentAppearance.Content == currentFrameQrContent)
            {
                currentAppearance = currentAppearance with { DurationInSeconds = currentAppearance.DurationInSeconds + 1 };
            }
            else
            {
                if (currentAppearance != null)
                {
                    results.Add(currentAppearance);
                }

                if (currentFrameQrContent != null)
                {
                    currentAppearance = new QrCodeResponse(currentTime, currentFrameQrContent, 1);
                }
                else
                {
                    currentAppearance = null;
                }
            }
        }

        if (currentAppearance != null)
        {
            results.Add(currentAppearance);
        }

        return results;
    }

    #endregion
}