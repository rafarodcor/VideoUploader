namespace VideoUploader.Tests.UnitTests
{
    public class VideoAnalysisTest
    {
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

        // Método auxiliar para validar o tipo do arquivo
        private bool ValidateFileType(string? fileName, string[] validExtensions)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var fileExtension = System.IO.Path.GetExtension(fileName).ToLower();
            return validExtensions.Contains(fileExtension);
        }

        #endregion
    }
}