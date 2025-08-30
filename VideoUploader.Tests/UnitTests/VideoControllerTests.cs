using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text;
using VideoUploader.API.Controllers;
using VideoUploader.Models.DTOs;
using VideoUploader.Models.Models;
using VideoUploader.Services.Persistence;

namespace VideoUploader.Tests.UnitTests;

public class VideoControllerTests
{
    #region Properties

    private readonly Mock<ILogger<VideoController>> _mockLogger;
    private readonly Mock<IVideoAnalysisService> _mockVideoAnalysisService;
    private readonly Mock<IOptions<FileStorageSettings>> _mockFileStorageSettings;
    private readonly VideoController _controller;

    #endregion

    #region Constructors

    public VideoControllerTests()
    {
        _mockLogger = new Mock<ILogger<VideoController>>();
        _mockVideoAnalysisService = new Mock<IVideoAnalysisService>();
        _mockFileStorageSettings = new Mock<IOptions<FileStorageSettings>>();

        var settings = new FileStorageSettings { VideoPath = "TestFiles" };
        _mockFileStorageSettings.Setup(s => s.Value).Returns(settings);

        _controller = new VideoController(
            _mockLogger.Object,
            _mockVideoAnalysisService.Object,
            _mockFileStorageSettings.Object);
    }

    #endregion

    #region Methods

    [Fact]
    [Trait("Category", "Unit")]
    public void Should_ReturnError_When_FileTypeIsInvalid()
    {
        // Arrange
        var fileName = "document.pdf";
        var validExtensions = new[] { ".mp4", ".avi", ".mkv" };

        // Act
        var isValid = ValidateFileType(fileName, validExtensions);

        // Assert
        Assert.False(isValid, "O tipo do arquivo deve ser inválido.");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Should_ReturnError_When_FileIsNull()
    {
        // Arrange
        string? fileName = null; // Arquivo nulo
        var validExtensions = new[] { ".mp4", ".avi", ".mkv" };

        // Act
        var isValid = ValidateFileType(fileName, validExtensions);

        // Assert
        Assert.False(isValid, "O arquivo sendo nulo deve ser inválido.");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadVideoAsync_WhenNoFilesProvided_ShouldReturnBadRequest()
    {
        // Arrange (Preparação)
        var emptyFileList = new List<IFormFile>();

        // Act (Ação)
        var result = await _controller.UploadVideoAsync(emptyFileList);

        // Assert (Verificação)
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Nenhum arquivo de vídeo foi enviado.", badRequestResult.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadVideoAsync_WhenFileHasInvalidExtension_ShouldReturnBadRequest()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("meu-video.txt");
        fileMock.Setup(f => f.Length).Returns(100); // Precisa ter um tamanho > 0
        var files = new List<IFormFile> { fileMock.Object };

        // Act
        var result = await _controller.UploadVideoAsync(files);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Formato de arquivo inválido", badRequestResult.Value?.ToString());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadVideoAsync_WithValidFile_ShouldReturnAcceptedAndCallService()
    {
        // Arrange
        // Criamos um IFormFile "fake" em memória
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("this is a fake video"));
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("video.mp4");
        fileMock.Setup(f => f.Length).Returns(stream.Length);
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
              .Returns((Stream target, CancellationToken token) => stream.CopyToAsync(target, token));

        var files = new List<IFormFile> { fileMock.Object };

        // Act
        var result = await _controller.UploadVideoAsync(files);

        // Assert
        // Verifica se o resultado HTTP é 202 Accepted
        var acceptedResult = Assert.IsType<AcceptedResult>(result);
        var responseValue = Assert.IsAssignableFrom<List<UploadResponse>>(acceptedResult.Value);
        Assert.Single(responseValue); // Deve haver uma resposta para o arquivo enviado
        Assert.Equal("Vídeo recebido e enfileirado para análise.", responseValue.First().Message);

        // Verifica se os métodos do nosso serviço foram chamados, provando que o fluxo deu certo
        _mockVideoAnalysisService.Verify(s => s.SaveAnalysisStatus(It.IsAny<VideoAnalysis>()), Times.Once);
        _mockVideoAnalysisService.Verify(s => s.UploadVideo(It.IsAny<InformationFile>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAnalysisStatus_WhenAnalysisNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _mockVideoAnalysisService
            .Setup(s => s.GetAnalysisStatus(nonExistentId))
            .ReturnsAsync((VideoAnalysis)null); // Simula o repositório não encontrando nada

        // Act
        var result = await _controller.GetAnalysisStatus(nonExistentId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("não encontrada", notFoundResult.Value?.ToString());
    }

    private bool ValidateFileType(string? fileName, string[] validExtensions)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        var fileExtension = System.IO.Path.GetExtension(fileName).ToLower();
        return validExtensions.Contains(fileExtension);
    }

    #endregion
}