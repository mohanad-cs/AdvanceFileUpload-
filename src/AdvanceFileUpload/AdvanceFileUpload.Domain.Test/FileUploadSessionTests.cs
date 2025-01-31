using System;
using System.Collections.Generic;
using System.IO;
using AdvanceFileUpload.Domain.Exception;
using Xunit;

namespace AdvanceFileUpload.Domain.Test
{
    public class FileUploadSessionTests
    {
        [Fact]
        public void Constructor_ValidParameters_ShouldInitializeCorrectly()
        {
            // Arrange
            string fileName = "testfile.txt";
            string savingDirectory = "C:\\Uploads";
            long fileSize = 1024;
            long maxChunkSize = 256;

            // Act
            var session = new FileUploadSession(fileName, savingDirectory, fileSize, maxChunkSize);

            // Assert
            Assert.Equal(fileName, session.FileName);
            Assert.Equal(savingDirectory, session.SavingDirectory);
            Assert.Equal(fileSize, session.FileSize);
            Assert.Equal(maxChunkSize, session.MaxChunkSize);
            Assert.Equal(FileUploadSessionStatus.InProgress, session.Status);
            Assert.NotEqual(default(DateTime), session.SessionStartDate);
        }

        [Fact]
        public void Constructor_InvalidParameters_ShouldThrowArgumentException()
        {
            // Arrange
            string fileName = "";
            string savingDirectory = "";
            long fileSize = 0;
            long maxChunkSize = 0;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new FileUploadSession(fileName, savingDirectory, fileSize, maxChunkSize));
        }

        [Fact]
        public void AddChunk_ValidChunk_ShouldAddChunk()
        {
            // Arrange
            var session = CreateValidSession();
            int chunkIndex = 0;
            string chunkPath = "C:\\Uploads\\chunk0";

            // Act
            session.AddChunk(chunkIndex, chunkPath);

            // Assert
            Assert.Single(session.ChunkFiles);
            Assert.Equal(FileUploadSessionStatus.InProgress, session.Status);
        }

        [Fact]
        public void AddChunk_ChunkAlreadyUploaded_ShouldThrowException()
        {
            // Arrange
            var session = CreateValidSession();
            int chunkIndex = 0;
            string chunkPath = "C:\\Uploads\\chunk0";
            session.AddChunk(chunkIndex, chunkPath);

            // Act & Assert
            Assert.Throws<ChunkUploadingException>(() => session.AddChunk(chunkIndex, chunkPath));
        }

        [Fact]
        public void AddChunk_SessionCompleted_ShouldThrowException()
        {
            // Arrange
            var session = CreateValidSession();
            session.CompleteSession();
            int chunkIndex = 0;
            string chunkPath = "C:\\Uploads\\chunk0";

            // Act & Assert
            Assert.Throws<ChunkUploadingException>(() => session.AddChunk(chunkIndex, chunkPath));
        }

        [Fact]
        public void GetRemainChunks_ShouldReturnCorrectRemainingChunks()
        {
            // Arrange
            var session = CreateValidSession();
            session.AddChunk(0, "C:\\Uploads\\chunk0");

            // Act
            var remainChunks = session.GetRemainChunks();

            // Assert
            Assert.Equal(new List<int> { 1, 2, 3 }, remainChunks);
        }

        [Fact]
        public void IsAllChunkUploaded_ShouldReturnTrueWhenAllChunksUploaded()
        {
            // Arrange
            var session = CreateValidSession();
            for (int i = 0; i < session.TotalChunksToUpload; i++)
            {
                session.AddChunk(i, $"C:\\Uploads\\chunk{i}");
            }

            // Act
            var result = session.IsAllChunkUploaded();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsChunkUploaded_ShouldReturnTrueForUploadedChunk()
        {
            // Arrange
            var session = CreateValidSession();
            session.AddChunk(0, "C:\\Uploads\\chunk0");

            // Act
            var result = session.IsChunkUploaded(0);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsCompleted_ShouldReturnTrueWhenSessionCompleted()
        {
            // Arrange
            var session = CreateValidSession();
            for (int i = 0; i < session.TotalChunksToUpload; i++)
            {
                session.AddChunk(i, $"C:\\Uploads\\chunk{i}");
            }
            session.CompleteSession();

            // Act
            var result = session.IsCompleted();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsCanceled_ShouldReturnTrueWhenSessionCanceled()
        {
            // Arrange
            var session = CreateValidSession();
            session.CancelSession();

            // Act
            var result = session.IsCanceled();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CancelSession_ShouldCancelSession()
        {
            // Arrange
            var session = CreateValidSession();

            // Act
            session.CancelSession();

            // Assert
            Assert.Equal(FileUploadSessionStatus.Canceled, session.Status);
            Assert.NotNull(session.SessionEndDate);
        }

        [Fact]
        public void PauseSession_ShouldPauseSession()
        {
            // Arrange
            var session = CreateValidSession();

            // Act
            session.PauseSession();

            // Assert
            Assert.Equal(FileUploadSessionStatus.Paused, session.Status);
            Assert.NotNull(session.SessionEndDate);
        }

        [Fact]
        public void CompleteSession_ShouldCompleteSession()
        {
            // Arrange
            var session = CreateValidSession();
            for (int i = 0; i < session.TotalChunksToUpload; i++)
            {
                session.AddChunk(i, $"C:\\Uploads\\chunk{i}");
            }

            // Act
            session.CompleteSession();

            // Assert
            Assert.Equal(FileUploadSessionStatus.Completed, session.Status);
            Assert.NotNull(session.SessionEndDate);
        }

        private FileUploadSession CreateValidSession()
        {
            string fileName = "testfile.txt";
            string savingDirectory = "C:\\Uploads";
            long fileSize = 1024;
            long maxChunkSize = 256;
            return new FileUploadSession(fileName, savingDirectory, fileSize, maxChunkSize);
        }
    }
}
