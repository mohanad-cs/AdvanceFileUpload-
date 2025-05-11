using AdvanceFileUpload.Application;
using AdvanceFileUpload.Application.Compression;
using AdvanceFileUpload.Application.FileProcessing;
using AdvanceFileUpload.Application.Request;
using AdvanceFileUpload.Application.Response;
using AdvanceFileUpload.Application.Settings;
using AdvanceFileUpload.Application.Validators;
using AdvanceFileUpload.Domain.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AdvanceFileUpload.Domain.Test
{
    public class UploadMangerTests
    {


        private readonly Mock<IRepository<FileUploadSession>> _repositoryMock;
        private readonly Mock<IDomainEventPublisher> _domainEventPublisherMock;
        private readonly Mock<FileValidator> _fileValidatorMock;
        private readonly Mock<ChunkValidator> _chunkValidatorMock;
        private readonly FileProcessor _fileProcessorMock;
        private readonly FileCompressor _fileCompressorMock;
        private readonly Mock<ILogger<UploadManger>> _loggerMock;
        private readonly IOptions<UploadSetting> _uploadSetting;

        public UploadMangerTests()
        {
            TestsUtility.InsureTestDataCreated();
            _repositoryMock = new Mock<IRepository<FileUploadSession>>();
            _domainEventPublisherMock = new Mock<IDomainEventPublisher>();
            _fileValidatorMock = new Mock<FileValidator>();
            _chunkValidatorMock = new Mock<ChunkValidator>();
            _fileProcessorMock = new FileProcessor(NullLogger<FileProcessor>.Instance);
            _loggerMock = new Mock<ILogger<UploadManger>>();
            _fileCompressorMock = new FileCompressor(NullLogger<FileCompressor>.Instance);
            _uploadSetting = Options.Create(new UploadSetting
            {
                SavingDirectory = TestsUtility._tempDirectory,
                MaxFileSize = 1024 * 1024 * 10,
                AllowedExtensions = new[] { ".pdf" },
                MaxChunkSize = 1024 * 1024,
                TempDirectory = TestsUtility._tempDirectory
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
                _fileProcessorMock,
                _fileCompressorMock,
                _loggerMock.Object);

            var request = new CreateUploadSessionRequest
            {
                FileName = TestsUtility._fileName,
                FileSize = TestsUtility._fileSize,
                FileExtension = ".pdf",
                CompressedFileSize = null,

            };

            // Act
            var response = await uploadManager.CreateUploadSessionAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(request.FileSize, response.FileSize);
            Assert.Equal(_uploadSetting.Value.MaxChunkSize, response.MaxChunkSize);
            Assert.Equal(UploadStatus.InProgress, response.UploadStatus);
        }

        [Fact]
        public async Task CompleteUploadSessionAsync_ShouldCompleteSession()
        {
            // Arrange
            var session =TestsUtility.GetValidAllChunkUploadedNotCompletedFileUploadSession();
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);
            var uploadManager = new UploadManger(
                _repositoryMock.Object,
                _domainEventPublisherMock.Object,
                _fileValidatorMock.Object,
                _chunkValidatorMock.Object,
                _uploadSetting,
                _fileProcessorMock,
                _fileCompressorMock,
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
            var session = new FileUploadSession(TestsUtility._fileName, TestsUtility._tempDirectory, TestsUtility._fileSize, null, 256 * 1024);
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
                _fileProcessorMock,
                _fileCompressorMock,
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
            var session = TestsUtility.GetValidAllChunkUploadedNotCompletedFileUploadSession();
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var uploadManager = new UploadManger(
                _repositoryMock.Object,
                _domainEventPublisherMock.Object,
                _fileValidatorMock.Object,
                _chunkValidatorMock.Object,
                _uploadSetting,
                _fileProcessorMock,
                _fileCompressorMock,
                _loggerMock.Object);

            // Act
            var response = await uploadManager.GetUploadSessionStatusAsync(session.Id);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(session.Id, response.SessionId);
            Assert.Equal(session.FileSize, response.FileSize);
            Assert.Equal(session.MaxChunkSize, response.MaxChunkSize);
            Assert.Equal(session.TotalChunksToUpload, response.TotalChunksToUpload);
            Assert.Equal(session.Status, (FileUploadSessionStatus)response.UploadStatus);
        }

        [Fact]
        public async Task CancelUploadSessionAsync_ShouldCancelSession()
        {
            // Arrange
            var session = TestsUtility.GetFileUploadSessionWithRemainingChunks();
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var uploadManager = new UploadManger(
                _repositoryMock.Object,
                _domainEventPublisherMock.Object,
                _fileValidatorMock.Object,
                _chunkValidatorMock.Object,
                _uploadSetting,
                _fileProcessorMock,
                _fileCompressorMock,
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
            var session = TestsUtility.GetFileUploadSessionWithRemainingChunks();
            _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(session);

            var uploadManager = new UploadManger(
                _repositoryMock.Object,
                _domainEventPublisherMock.Object,
                _fileValidatorMock.Object,
                _chunkValidatorMock.Object,
                _uploadSetting,
                _fileProcessorMock,
                _fileCompressorMock,
                _loggerMock.Object);

            // Act
            var result = await uploadManager.PauseUploadSessionAsync(session.Id);

            // Assert
            Assert.True(result);
            Assert.Equal(FileUploadSessionStatus.Paused, session.Status);
        }


       
    }

}
