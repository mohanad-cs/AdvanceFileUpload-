using AdvanceFileUpload.Application.FileProcessing;

namespace AdvanceFileUpload.Domain.Test
{
    public class FileProcessorTests
    {
        private const string TestFilePath = @"D:\University\AdvanceFileUpload-\src\AdvanceFileUpload\AdvanceFileUpload.Domain.Test\TestFiles\testFile.Pdf";
        private const string TempDirectory = @"D:\University\AdvanceFileUpload-\src\AdvanceFileUpload\AdvanceFileUpload.Domain.Test\Temp\";

        [Fact]
        public async Task SaveFileAsync_ShouldSaveFile()
        {
            // Arrange
            var fileProcessor = new FileProcessor();
            string fileName = "testFile.Pdf";
            byte[] fileData = await File.ReadAllBytesAsync(TestFilePath);
            string filePath = Path.Combine(TempDirectory, fileName);

            // Act
            await fileProcessor.SaveFileAsync(fileName, fileData, TempDirectory);

            // Assert
            Assert.True(File.Exists(filePath));
            var savedData = await File.ReadAllBytesAsync(filePath);
            Assert.Equal(fileData, savedData);

            // Cleanup
            File.Delete(filePath);
        }

        [Fact]
        public async Task SplitFileIntoChunksAsync_ShouldSplitFile()
        {
            // Arrange
            var fileProcessor = new FileProcessor();
            string filePath = TestFilePath;
            long chunkSize = 256 * 1024; // 256 KB
            int expectedChunks = (int)Math.Ceiling((double)new FileInfo(TestFilePath).Length / chunkSize);
            // Act
            var chunkPaths = await fileProcessor.SplitFileIntoChunksAsync(filePath, chunkSize, TempDirectory);

            // Assert
            Assert.Equal(expectedChunks, chunkPaths.Count);
            foreach (var chunkPath in chunkPaths)
            {
                Assert.True(File.Exists(chunkPath));
            }

           // Cleanup
            foreach (var chunkPath in chunkPaths)
            {
                File.Delete(chunkPath);
            }
        }

        [Fact]
        public async Task ConcatenateChunksAsync_ShouldConcatenateChunks()
        {
            // Arrange
            var fileProcessor = new FileProcessor();
            string outputFilePath = Path.Combine(TempDirectory, "concatenated.Pdf");
            string chunk1Path = Path.Combine(TempDirectory, "chunk1.Pdf");
            string chunk2Path = Path.Combine(TempDirectory, "chunk2.Pdf");
            await File.WriteAllBytesAsync(chunk1Path, new byte[] { 1, 2 });
            await File.WriteAllBytesAsync(chunk2Path, new byte[] { 3, 4 });
            var chunkPaths = new List<string> { chunk1Path, chunk2Path };

            // Act
            await fileProcessor.ConcatenateChunksAsync(chunkPaths, outputFilePath);

            // Assert
            Assert.True(File.Exists(outputFilePath));
            var concatenatedData = await File.ReadAllBytesAsync(outputFilePath);
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, concatenatedData);

            // Cleanup
            File.Delete(outputFilePath);
            File.Delete(chunk1Path);
            File.Delete(chunk2Path);
        }
    }

}
