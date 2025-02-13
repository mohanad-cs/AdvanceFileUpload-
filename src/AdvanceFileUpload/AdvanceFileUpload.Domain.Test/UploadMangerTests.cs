using AdvanceFileUpload.Application;
using AdvanceFileUpload.Application.FileProcessing;
using AdvanceFileUpload.Application.Request;
using AdvanceFileUpload.Application.Response;
using AdvanceFileUpload.Application.Settings;
using AdvanceFileUpload.Application.Validators;
using AdvanceFileUpload.Domain.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AdvanceFileUpload.Domain.Test
{
    public class UploadMangerTests
    {
        private const string TestFilePath = @"D:\University\AdvanceFileUpload-\src\AdvanceFileUpload\AdvanceFileUpload.Domain.Test\TestFiles\testFile.Pdf";
        private const string TempDirectory = @"D:\University\AdvanceFileUpload-\src\AdvanceFileUpload\AdvanceFileUpload.Domain.Test\Temp\";

        private readonly Mock<IRepository<FileUploadSession>> _repositoryMock;
        private readonly Mock<IDomainEventPublisher> _domainEventPublisherMock;
        private readonly Mock<IFileValidator> _fileValidatorMock;
        private readonly Mock<IChunkValidator> _chunkValidatorMock;
        private readonly Mock<IFileProcessor> _fileProcessorMock;
        private readonly Mock<ILogger<UploadManger>> _loggerMock;
        private readonly IOptions<UploadSetting> _uploadSetting;

        public UploadMangerTests()
        {
            _repositoryMock = new Mock<IRepository<FileUploadSession>>();
            _domainEventPublisherMock = new Mock<IDomainEventPublisher>();
            _fileValidatorMock = new Mock<IFileValidator>();
            _chunkValidatorMock = new Mock<IChunkValidator>();
            _fileProcessorMock = new Mock<IFileProcessor>();
            _loggerMock = new Mock<ILogger<UploadManger>>();
            _uploadSetting = Options.Create(new UploadSetting
            {
                SavingDirectory = TempDirectory,
                MaxFileSize = 1024 * 1024 * 10,
                AllowedExtensions = new[] { ".pdf" },
                MaxChunkSize = 1024 * 1024,
                TempDirectory = TempDirectory
            });
        }

        [Fact]
        public async Task CreateUploadSessionAsync_ShouldCreateSession()
        {
            // Arrange
            var uploadManager = new UploadManger(
                _repositoryMock.Object,
                _domainEventPublisherMock.Object,
                _fileValidatorMock.Object,
                _chunkValidatorMock.Object,
                _uploadSetting,
                _fileProcessorMock.Object,
                _loggerMock.Object);

            var request = new CreateUploadSessionRequest
            {
                FileName = "testFile.Pdf",
                FileSize = new FileInfo(TestFilePath).Length,
                FileExtension = ".pdf"
            };

            // Act
            var response = await uploadManager.CreateUploadSessionAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(request.FileSize, response.FileSize);
            Assert.Equal(_uploadSetting.Value.MaxChunkSize, response.MaxMaxChunkSize);
            Assert.Equal(UploadStatus.InProgress, response.UploadStatus);
        }

        [Fact]
        public async Task CompleteUploadSessionAsync_ShouldCompleteSession()
        {
            // Arrange
            var session = new FileUploadSession("testFile.Pdf", TempDirectory, new FileInfo(TestFilePath).Length, 256 * 1024);
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var uploadManager = new UploadManger(
                _repositoryMock.Object,
                _domainEventPublisherMock.Object,
                _fileValidatorMock.Object,
                _chunkValidatorMock.Object,
                _uploadSetting,
                _fileProcessorMock.Object,
                _loggerMock.Object);

            // Act
            var result = await uploadManager.CompleteUploadSessionAsync(session.Id);

            // Assert
            Assert.True(result);
            Assert.Equal(FileUploadSessionStatus.Completed, session.Status);
        }

        [Fact]
        public async Task UploadChunkAsync_ShouldUploadChunk()
        {
            // Arrange
            var session = new FileUploadSession("testFile.Pdf", TempDirectory, new FileInfo(TestFilePath).Length, 256 * 1024);
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var uploadManager = new UploadManger(
                _repositoryMock.Object,
                _domainEventPublisherMock.Object,
                _fileValidatorMock.Object,
                _chunkValidatorMock.Object,
                _uploadSetting,
                _fileProcessorMock.Object,
                _loggerMock.Object);

            var request = new UploadChunkRequest
            {
                SessionId = session.Id,
                ChunkIndex = 0,
                ChunkData = new byte[256 * 1024]
            };

            // Act
            var result = await uploadManager.UploadChunkAsync(request);

            // Assert
            Assert.True(result);
            Assert.Single(session.ChunkFiles);
            Assert.Equal(request.ChunkIndex, session.ChunkFiles.First().ChunkIndex);
        }

        [Fact]
        public async Task GetUploadSessionStatusAsync_ShouldReturnStatus()
        {
            // Arrange
            var session = new FileUploadSession("testFile.Pdf", TempDirectory, new FileInfo(TestFilePath).Length, 256 * 1024);
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var uploadManager = new UploadManger(
                _repositoryMock.Object,
                _domainEventPublisherMock.Object,
                _fileValidatorMock.Object,
                _chunkValidatorMock.Object,
                _uploadSetting,
                _fileProcessorMock.Object,
                _loggerMock.Object);

            // Act
            var response = await uploadManager.GetUploadSessionStatusAsync(session.Id);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(session.Id, response.SessionId);
            Assert.Equal(session.FileSize, response.FileSize);
            Assert.Equal(session.MaxChunkSize, response.MaxMaxChunkSize);
            Assert.Equal(session.TotalChunksToUpload, response.TotalChunksToUpload);
            Assert.Equal(session.Status, (FileUploadSessionStatus)response.UploadStatus);
        }

        [Fact]
        public async Task CancelUploadSessionAsync_ShouldCancelSession()
        {
            // Arrange
            var session = new FileUploadSession("testFile.Pdf", TempDirectory, new FileInfo(TestFilePath).Length, 256 * 1024);
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var uploadManager = new UploadManger(
                _repositoryMock.Object,
                _domainEventPublisherMock.Object,
                _fileValidatorMock.Object,
                _chunkValidatorMock.Object,
                _uploadSetting,
                _fileProcessorMock.Object,
                _loggerMock.Object);

            // Act
            var result = await uploadManager.CancelUploadSessionAsync(session.Id);

            // Assert
            Assert.True(result);
            Assert.Equal(FileUploadSessionStatus.Canceled, session.Status);
        }

        [Fact]
        public async Task PauseUploadSessionAsync_ShouldPauseSession()
        {
            // Arrange
            var session = new FileUploadSession("testFile.Pdf", TempDirectory, new FileInfo(TestFilePath).Length, 256 * 1024);
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var uploadManager = new UploadManger(
                _repositoryMock.Object,
                _domainEventPublisherMock.Object,
                _fileValidatorMock.Object,
                _chunkValidatorMock.Object,
                _uploadSetting,
                _fileProcessorMock.Object,
                _loggerMock.Object);

            // Act
            var result = await uploadManager.PauseUploadSessionAsync(session.Id);

            // Assert
            Assert.True(result);
            Assert.Equal(FileUploadSessionStatus.Paused, session.Status);
        }
    }

}
