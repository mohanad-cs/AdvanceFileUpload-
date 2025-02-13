using AdvanceFileUpload.Domain.Exceptions;

namespace AdvanceFileUpload.Domain.Test
{
    public class FileUploadSessionTests
    {
        private const string TestFilePath = @"D:\University\AdvanceFileUpload-\src\AdvanceFileUpload\AdvanceFileUpload.Domain.Test\TestFiles\testFile.Pdf";
        private const string TempDirectory = @"D:\University\AdvanceFileUpload-\src\AdvanceFileUpload\AdvanceFileUpload.Domain.Test\Temp\";

        [Fact]
        public void Constructor_ShouldInitializeProperties()
        {
            // Arrange
            string fileName = "testFile.Pdf";
            long fileSize = new FileInfo(TestFilePath).Length;
            long maxChunkSize = 256 * 1024; // 256 KB

            // Act
            var session = new FileUploadSession(fileName, TempDirectory, fileSize, maxChunkSize);

            // Assert
            Assert.Equal(fileName, session.FileName);
            Assert.Equal(TempDirectory, session.SavingDirectory);
            Assert.Equal(fileSize, session.FileSize);
            Assert.Equal(maxChunkSize, session.MaxChunkSize);
            Assert.Equal(FileUploadSessionStatus.InProgress, session.Status);
            Assert.NotNull(session.SessionStartDate);
        }

        [Fact]
        public void AddChunk_ShouldAddChunk()
        {
            // Arrange
            var session = new FileUploadSession("testFile.Pdf", TempDirectory, new FileInfo(TestFilePath).Length, 256 * 1024);
            int chunkIndex = 0;
            string chunkPath = Path.Combine(TempDirectory, "chunk0.Pdf");

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
            var session = new FileUploadSession("testFile.Pdf", TempDirectory, new FileInfo(TestFilePath).Length, 256 * 1024);
            string chunkPath = Path.Combine(TempDirectory, "chunk0.Pdf");
            for (int i = 0; i < session.TotalChunksToUpload; i++)
            {
                session.AddChunk(i, chunkPath);
            }
            session.CompleteSession();

            // Act & Assert
            Assert.Throws<ChunkUploadingException>(() => session.AddChunk(0, Path.Combine(TempDirectory, chunkPath)));
        }

        [Fact]
        public void GetRemainChunks_ShouldReturnRemainingChunks()
        {
            // Arrange
            long chunkSize = 256 * 1024; // 256 KB
            int expectedChunks = (int)Math.Ceiling((double)new FileInfo(TestFilePath).Length / chunkSize);
            var session = new FileUploadSession("testFile.Pdf", TempDirectory, new FileInfo(TestFilePath).Length, chunkSize);
            string chunkPath = Path.Combine(TempDirectory, "chunk0.Pdf");
            session.AddChunk(0, Path.Combine(TempDirectory, chunkPath));
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
            var session = new FileUploadSession("testFile.Pdf", TempDirectory, new FileInfo(TestFilePath).Length, 256 * 1024);

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
            var session = new FileUploadSession("testFile.Pdf", TempDirectory, new FileInfo(TestFilePath).Length, 256 * 1024);

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
            string chunkPath = Path.Combine(TempDirectory, "chunk0.Pdf");
            long chunkSize = 256 * 1024; // 256 KB
            int expectedChunks = (int)Math.Ceiling((double)new FileInfo(TestFilePath).Length / chunkSize);
            var session = new FileUploadSession("testFile.Pdf", TempDirectory, new FileInfo(TestFilePath).Length,chunkSize);
            for (int i = 0; i < expectedChunks; i++)
            {
                session.AddChunk(i, Path.Combine(TempDirectory, chunkPath));

            }
            // Act
            session.CompleteSession();

            // Assert
            Assert.Equal(FileUploadSessionStatus.Completed, session.Status);
            Assert.NotNull(session.SessionEndDate);
        }
    }

}
