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
        private const string _testFilePath = @"..\TestFiles\testFile.Pdf";
        private const string _tempDirectory = "..\\Temp\\";
        private string _fileName = Path.GetFileName(_testFilePath);
        private long _fileSize = new FileInfo(_testFilePath).Length;
        private long _maxChunkSize = 256 * 1024; // 256 KB

        private readonly Mock<IRepository<FileUploadSession>> _repositoryMock;
        private readonly Mock<IDomainEventPublisher> _domainEventPublisherMock;
        private readonly Mock<FileValidator> _fileValidatorMock;
        private readonly Mock<ChunkValidator> _chunkValidatorMock;
        private readonly Mock<FileProcessor> _fileProcessorMock;
        private readonly Mock<ILogger<UploadManger>> _loggerMock;
        private readonly IOptions<UploadSetting> _uploadSetting;

        public UploadMangerTests()
        {
            _repositoryMock = new Mock<IRepository<FileUploadSession>>();
            _domainEventPublisherMock = new Mock<IDomainEventPublisher>();
            _fileValidatorMock = new Mock<FileValidator>();
            _chunkValidatorMock = new Mock<ChunkValidator>();
            _fileProcessorMock = new Mock<FileProcessor>();
            _loggerMock = new Mock<ILogger<UploadManger>>();
            _uploadSetting = Options.Create(new UploadSetting
            {
                SavingDirectory = _tempDirectory,
                MaxFileSize = 1024 * 1024 * 10,
                AllowedExtensions = new[] { ".pdf" },
                MaxChunkSize = 1024 * 1024,
                TempDirectory = _tempDirectory
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
                FileName = _fileName,
                FileSize = _fileSize,
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
            var session = GetValidAllChunkUploadedNotCompletedFileUploadSession();
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
            var session = new FileUploadSession(_fileName, _tempDirectory, _fileSize, 256 * 1024);
            var request = new UploadChunkRequest
            {
                SessionId = session.Id,
                ChunkIndex = 0,
                ChunkData = new byte[256 * 1024]

            };
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

           // await File.WriteAllBytesAsync(Path.Combine(_uploadSetting.Value.TempDirectory, $"{session.Id}_{request.ChunkIndex}.chunk"), request.ChunkData);

            var uploadManager = new UploadManger(
                _repositoryMock.Object,
                _domainEventPublisherMock.Object,
                _fileValidatorMock.Object,
                _chunkValidatorMock.Object,
                _uploadSetting,
                _fileProcessorMock.Object,
                _loggerMock.Object);



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
            var session = GetValidAllChunkUploadedNotCompletedFileUploadSession();
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
            var session = GetFileUploadSessionWithRemainingChunks();
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
            var session = GetFileUploadSessionWithRemainingChunks();
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


        private FileUploadSession GetValidAllChunkUploadedNotCompletedFileUploadSession()
        {
            FileUploadSession fileUploadSession = new FileUploadSession(_fileName, _tempDirectory, _fileSize, _maxChunkSize);
            for (int i = 0; i < fileUploadSession.TotalChunksToUpload; i++)
            {
                fileUploadSession.AddChunk(i, Path.Combine(_tempDirectory, "chunk0.Pdf"));
            }
            return fileUploadSession;
        }
        private FileUploadSession GetFileUploadSessionWithRemainingChunks()
        {
            FileUploadSession fileUploadSession = new FileUploadSession(_fileName, _tempDirectory, _fileSize, _maxChunkSize);
            for (int i = 0; i < fileUploadSession.TotalChunksToUpload - 1; i++)
            {
                fileUploadSession.AddChunk(i, Path.Combine(_tempDirectory, "chunk0.Pdf"));
            }
            return fileUploadSession;
        }
    }

}
