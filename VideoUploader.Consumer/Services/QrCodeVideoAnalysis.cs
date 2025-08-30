using FFMpegCore;
using Microsoft.Extensions.Options;
using System.Drawing;
using VideoUploader.Models.Models;
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

    public async Task<List<(TimeSpan? Timestamp, string? QrCodeContent)>> FindQrCodeInVideoAsync(string videoPath)
    {
        var listTimestamps = new List<(TimeSpan? Timestamp, string? QrCodeContent)>();

        // Passo 1: Obter a duração total do vídeo para saber até onde iterar
        var mediaInfo = await FFProbe.AnalyseAsync(videoPath);
        var duration = mediaInfo.Duration;

        var reader = new BarcodeReader();

        Directory.CreateDirectory(_fileStorageSettings.VideoPath);

        // Passo 2: Iterar pelo vídeo, extraindo um frame por segundo
        for (double seconds = 0; seconds < duration.TotalSeconds; seconds++)
        {
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
                    // Adiciona o timestamp e o conteúdo do QR Code na lista de retorno
                    listTimestamps.Add((currentTime, result.Text));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao processar o frame no timestamp {Timestamp} do vídeo {VideoPath}. Continuando a análise.", currentTime, videoPath);
            }
            finally
            {
                // Garante que o arquivo temporário seja deletado
                if (File.Exists(tempImagePath))
                {
                    File.Delete(tempImagePath);
                }
            }
        }

        return listTimestamps;
    }

    //public async Task SaveImageFrameVideo()
    //{
    //    // Salva um frame do vídeo no tempo 00:01:30
    //    FFMpeg.Snapshot("caminho/para/seu_video.mp4", "caminho/para/thumbnail.png", new Size(640, 360), TimeSpan.FromMinutes(1.5));
    //}

    #endregion
}