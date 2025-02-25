using AdvanceFileUpload.Application.Validators;

namespace AdvanceFileUpload.Domain.Test
{
    public class FileValidatorTests
    {
        private readonly FileValidator _fileValidator;

        public FileValidatorTests()
        {
            _fileValidator = new FileValidator();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("file:.pdf")]
        [InlineData("file?.pdf")]
        [InlineData("fi\\le.pdf")]
        public void IsValidateFileName_InvalidFileName_ReturnsFalse(string fileName)
        {
            var result = _fileValidator.IsValidFileName(fileName);
            Assert.False(result);
        }

        [Theory]
        [InlineData("validFileName.txt")]
        [InlineData("validFileName.pdf")]
        public void IsValidateFileName_ValidFileName_ReturnsTrue(string fileName)
        {
            var result = _fileValidator.IsValidFileName(fileName);
            Assert.True(result);
        }

        [Theory]
        [InlineData(10, 0)]
        [InlineData(0, 0)]
        [InlineData(0, 100)]
        [InlineData(-1, 100)]
        [InlineData(101, 100)]
        public void IsValidateFileSize_InvalidFileSize_ReturnsFalse(long fileSize, long maxFileSize)
        {
            var result = _fileValidator.IsValidFileSize(fileSize, maxFileSize);
            Assert.False(result);
        }

        [Theory]
        [InlineData(50, 100)]
        [InlineData(100, 100)]
        public void IsValidateFileSize_ValidFileSize_ReturnsTrue(long fileSize, long maxFileSize)
        {
            var result = _fileValidator.IsValidFileSize(fileSize, maxFileSize);
            Assert.True(result);
        }

        [Theory]
        [InlineData(null, new string[] { ".txt", ".pdf" })]
        [InlineData("", new string[] { ".txt", ".pdf" })]
        [InlineData(".", new string[] { ".txt", ".pdf" })]
        [InlineData(".exe", new string[] { ".txt", ".pdf" })]
        [InlineData(".txt", null)]
        [InlineData(".txt", new string[] { })]
        public void IsValidateFileExtension_InvalidFileExtension_ReturnsFalse(string fileExtension, string[] allowedExtensions)
        {
            var result = _fileValidator.IsValidFileExtension(fileExtension, allowedExtensions);
            Assert.False(result);
        }

        [Theory]
        [InlineData(".txt", new string[] { ".txt", ".pdf" })]
        public void IsValidateFileExtension_ValidFileExtension_ReturnsTrue(string fileExtension, string[] allowedExtensions)
        {
            var result = _fileValidator.IsValidFileExtension(fileExtension, allowedExtensions);
            Assert.True(result);
        }
    }
}
