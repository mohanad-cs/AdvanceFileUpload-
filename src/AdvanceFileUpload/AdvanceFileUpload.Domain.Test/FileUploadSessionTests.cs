using AdvanceFileUpload.Domain.Exceptions;

namespace AdvanceFileUpload.Domain.Test
{
    public class FileUploadSessionTests
    {
        private const string _testFilePath = @"..\TestFiles\testFile.Pdf";
        private const string _tempDirectory = @"..\Temp\";
        private string _fileName = Path.GetFileName(_testFilePath);
        private long _fileSize = new FileInfo(_testFilePath).Length;
        private long _maxChunkSize = 256 * 1024; // 256 KB

        [Fact]
        public void Constructor_ShouldInitializeProperties()
        {
            // Arrange
            // Act
            var session = new FileUploadSession(_fileName, _tempDirectory, _fileSize, null, _maxChunkSize);


            // Assert
            Assert.Equal(_fileName, session.FileName);
            Assert.Equal(_tempDirectory, session.SavingDirectory);
            Assert.Equal(_fileSize, session.FileSize);
            Assert.Equal(_maxChunkSize, session.MaxChunkSize);
            Assert.Equal(FileUploadSessionStatus.InProgress, session.Status);
            Assert.NotNull(session.SessionStartDate);
        }

        [Fact]
        public void AddChunk_ShouldAddChunk()
        {
            // Arrange
            var session = new FileUploadSession(_fileName, _tempDirectory, new FileInfo(_testFilePath).Length, null, _maxChunkSize);
            int chunkIndex = 0;
            string chunkPath = Path.Combine(_tempDirectory, "chunk0.Pdf");

            // Act
            session.AddChunk(chunkIndex, chunkPath);

            // Assert
            Assert.Single(session.ChunkFiles);
            Assert.Equal(chunkIndex, session.ChunkFiles.First().ChunkIndex);
            Assert.Equal(chunkPath, session.ChunkFiles.First().ChunkPath);
        }

        [Fact]
        public void AddChunk_ShouldThrowException_WhenSessionIsCompleted()
        {
            // Arrange
            var session = new FileUploadSession(_fileName, _tempDirectory, new FileInfo(_testFilePath).Length, null, _maxChunkSize);
            string chunkPath = Path.Combine(_tempDirectory, "chunk0.Pdf");
            for (int i = 0; i < session.TotalChunksToUpload; i++)
            {
                session.AddChunk(i, chunkPath);
            }
            session.CompleteSession();

            // Act & Assert
            Assert.Throws<ChunkUploadingException>(() => session.AddChunk(0, Path.Combine(_tempDirectory, chunkPath)));
        }

        [Fact]
        public void GetRemainChunks_ShouldReturnRemainingChunks()
        {
            // Arrange
            long chunkSize = _maxChunkSize; // 256 KB
            int expectedChunks = (int)Math.Ceiling((double)new FileInfo(_testFilePath).Length / chunkSize);
            var session = new FileUploadSession(_fileName, _tempDirectory, new FileInfo(_testFilePath).Length, null, chunkSize);
            string chunkPath = Path.Combine(_tempDirectory, "chunk0.Pdf");
            session.AddChunk(0, Path.Combine(_tempDirectory, chunkPath));
            int expectedRemainingChunks = expectedChunks - 1;
            // Act
            var remainChunks = session.GetRemainChunks();

            // Assert
            Assert.Equal(expectedRemainingChunks, remainChunks.Count);
            for (int i = 1; i < expectedRemainingChunks; i++)
            {
                Assert.Contains(i, remainChunks);
            }

        }

        [Fact]
        public void CancelSession_ShouldUpdateStatus()
        {
            // Arrange
            var session = new FileUploadSession(_fileName, _tempDirectory, new FileInfo(_testFilePath).Length, null, _maxChunkSize);

            // Act
            session.CancelSession();

            // Assert
            Assert.Equal(FileUploadSessionStatus.Canceled, session.Status);
            Assert.NotNull(session.SessionEndDate);
        }

        [Fact]
        public void PauseSession_ShouldUpdateStatus()
        {
            // Arrange
            var session = new FileUploadSession(_fileName, _tempDirectory, new FileInfo(_testFilePath).Length, null, _maxChunkSize);

            // Act
            session.PauseSession();

            // Assert
            Assert.Equal(FileUploadSessionStatus.Paused, session.Status);
            Assert.NotNull(session.SessionEndDate);
        }

        [Fact]
        public void CompleteSession_ShouldUpdateStatus()
        {
            // Arrange
            string chunkPath = Path.Combine(_tempDirectory, "chunk0.Pdf");
            long chunkSize = _maxChunkSize; // 256 KB
            int expectedChunks = (int)Math.Ceiling((double)new FileInfo(_testFilePath).Length / chunkSize);
            var session = new FileUploadSession(_fileName, _tempDirectory, new FileInfo(_testFilePath).Length, null, chunkSize);
            for (int i = 0; i < expectedChunks; i++)
            {
                session.AddChunk(i, Path.Combine(_tempDirectory, chunkPath));

            }
            // Act
            session.CompleteSession();

            // Assert
            Assert.Equal(FileUploadSessionStatus.Completed, session.Status);
            Assert.NotNull(session.SessionEndDate);
        }
    }

}
