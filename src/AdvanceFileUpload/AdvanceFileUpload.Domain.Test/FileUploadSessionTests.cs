
using AdvanceFileUpload.Domain.Core;

namespace AdvanceFileUpload.Domain.Test
{
    public class FileUploadSessionTests
    {

        public FileUploadSessionTests()
        {
            TestsUtility.InsureTestDataCreated();
        }
        [Fact]
        public void Constructor_ShouldInitializeProperties()
        {
            // Arrange
            // Act
            var session = new FileUploadSession(TestsUtility._fileName, TestsUtility._tempDirectory, TestsUtility._fileSize, null, TestsUtility._maxChunkSize);


            // Assert
            Assert.Equal(TestsUtility._fileName, session.FileName);
            Assert.Equal(TestsUtility._tempDirectory, session.SavingDirectory);
            Assert.Equal(TestsUtility._fileSize, session.FileSize);
            Assert.Equal(TestsUtility._maxChunkSize, session.MaxChunkSize);
            Assert.Equal(FileUploadSessionStatus.InProgress, session.Status);
            Assert.True(session.SessionStartDate != default);
            Assert.Null(session.SessionEndDate);
        }

        [Fact]
        public void AddChunk_ShouldAddChunk()
        {
            // Arrange
            var session = new FileUploadSession(TestsUtility._fileName, TestsUtility._tempDirectory, new FileInfo(TestsUtility._pdfTestFilePath).Length, null, TestsUtility._maxChunkSize);
            int chunkIndex = 0;
            string chunkPath = TestsUtility._testChunkPath;

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
            //var session = new FileUploadSession(TestsUtility._fileName, TestsUtility._tempDirectory, new FileInfo(TestsUtility._pdfTestFilePath).Length, null, TestsUtility._maxChunkSize);
            string chunkPath = Path.Combine(TestsUtility._tempDirectory, "chunk0.Pdf");
            //for (int i = 0; i < session.TotalChunksToUpload; i++)
            //{
            //    session.AddChunk(i, chunkPath);
            //}
            //session.CompleteSession();
            var session = TestsUtility.GetValidAllChunkUploadedNotCompletedFileUploadSession();
            session.CompleteSession();
            // Act & Assert
            Assert.Throws<DomainException>(() => session.AddChunk(0, TestsUtility._testChunkPath));
        }

        [Fact]
        public void GetRemainChunks_ShouldReturnRemainingChunks()
        {
            // Arrange
            long chunkSize = TestsUtility._maxChunkSize; // 256 KB
            int expectedChunks = (int)Math.Ceiling((double)new FileInfo(TestsUtility._pdfTestFilePath).Length / chunkSize);
            var session = new FileUploadSession(TestsUtility._fileName, TestsUtility._tempDirectory, new FileInfo(TestsUtility._pdfTestFilePath).Length, null, chunkSize);
            session.AddChunk(0, TestsUtility._testChunkPath);
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
            var session = new FileUploadSession(TestsUtility._fileName, TestsUtility._tempDirectory, new FileInfo(TestsUtility._pdfTestFilePath).Length, null, TestsUtility._maxChunkSize);

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
            var session = new FileUploadSession(TestsUtility._fileName, TestsUtility._tempDirectory, new FileInfo(TestsUtility._pdfTestFilePath).Length, null, TestsUtility._maxChunkSize);

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
            var session = TestsUtility.GetValidAllChunkUploadedNotCompletedFileUploadSession();
            // Act
            session.CompleteSession();

            // Assert
            Assert.Equal(FileUploadSessionStatus.Completed, session.Status);
            Assert.True(session.IsCompleted());
            Assert.NotNull(session.SessionEndDate);
        }

        [Fact]
        public void MarkAsFailed_ShouldUpdateStatusToFailed()
        {
            // Arrange
            var session = new FileUploadSession(TestsUtility._fileName, TestsUtility._tempDirectory, TestsUtility._fileSize, null, TestsUtility._maxChunkSize);

            // Act
            session.MarkAsFailed();

            // Assert
            Assert.Equal(FileUploadSessionStatus.Failed, session.Status);
            Assert.True(session.IsFailed());
            Assert.NotNull(session.SessionEndDate);
        }

        [Fact]
        public void MarkAsFailed_ShouldThrowException_WhenSessionIsCompleted()
        {
            // Arrange
            var session = TestsUtility.GetValidAllChunkUploadedNotCompletedFileUploadSession();
            session.CompleteSession();

            // Act & Assert
            Assert.Throws<DomainException>(() => session.MarkAsFailed());
        }

        [Fact]
        public void MarkAsFailed_ShouldThrowException_WhenSessionIsCanceled()
        {
            // Arrange
            var session = new FileUploadSession(TestsUtility._fileName, TestsUtility._tempDirectory, TestsUtility._fileSize, null, TestsUtility._maxChunkSize);
            session.CancelSession();

            // Act & Assert
            Assert.Throws<DomainException>(() => session.MarkAsFailed());
        }





        [Fact]
        public void IsAllChunkUploaded_ShouldReturnTrue_WhenAllChunksAreUploaded()
        {
            // Arrange
            var session = TestsUtility.GetValidAllChunkUploadedNotCompletedFileUploadSession();


            // Act
            var allChunksUploaded = session.IsAllChunkUploaded();

            // Assert
            Assert.True(allChunksUploaded);
        }

        [Fact]
        public void IsAllChunkUploaded_ShouldReturnFalse_WhenNotAllChunksAreUploaded()
        {
            // Arrange
            var session = TestsUtility.GetFileUploadSessionWithRemainingChunks();
            // Act
            var allChunksUploaded = session.IsAllChunkUploaded();

            // Assert
            Assert.False(allChunksUploaded);
        }

        [Fact]
        public void IsChunkUploaded_ShouldReturnTrue_WhenChunkIsUploaded()
        {
            // Arrange
            var session = TestsUtility.GetValidAllChunkUploadedNotCompletedFileUploadSession();
            int lastChunkIndex = session.TotalChunksToUpload - 1;
            // Act
            var isChunkUploaded = session.IsChunkUploaded(0);
            var isLastChunkUploaded = session.IsChunkUploaded(lastChunkIndex);

            // Assert
            Assert.True(isChunkUploaded);
            Assert.True(isLastChunkUploaded);
        }

        [Fact]
        public void IsChunkUploaded_ShouldReturnFalse_WhenChunkIsNotUploaded()
        {
            // Arrange
            var session = new FileUploadSession(TestsUtility._fileName, TestsUtility._tempDirectory, TestsUtility._fileSize, null, TestsUtility._maxChunkSize);

            // Act
            var isChunkUploaded = session.IsChunkUploaded(0);

            // Assert
            Assert.False(isChunkUploaded);
        }

        [Fact]
        public void UseCompression_ShouldReturnTrue_WhenCompressionIsEnabled()
        {
            // Arrange
            var session = new FileUploadSession(TestsUtility._fileName, TestsUtility._tempDirectory, TestsUtility._fileSize, 1000, TestsUtility._maxChunkSize, CompressionAlgorithm.GZip, CompressionLevel.Optimal);

            // Act
            var useCompression = session.UseCompression;

            // Assert
            Assert.True(useCompression);
        }

        [Fact]
        public void UseCompression_ShouldReturnFalse_WhenCompressionIsNotEnabled()
        {
            // Arrange
            var session = new FileUploadSession(TestsUtility._fileName, TestsUtility._tempDirectory, TestsUtility._fileSize, null, TestsUtility._maxChunkSize);

            // Act
            var useCompression = session.UseCompression;

            // Assert
            Assert.False(useCompression);
        }
    }

}
