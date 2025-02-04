namespace AdvanceFileUpload.Application.Shared
{
    /// <summary>
    /// Provides methods for file operations such as concatenating file chunks, saving files, and splitting files into chunks.
    /// </summary>
    public class FileOperationService : IFileOperationService
    {
        /// <inheritdoc/>
        public async Task SaveFileAsync(string fileName, byte[] fileData, string directory, CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string filePath = Path.Combine(directory, fileName);
            await File.WriteAllBytesAsync(filePath, fileData);
        }
        /// <inheritdoc/>
        public async Task<List<string>> SplitFileIntoChunksAsync(string filePath, long chunkSize, string outputDirectory , CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The specified file does not exist.", filePath);
            }

            Directory.CreateDirectory(outputDirectory);

            List<string> chunkPaths = new List<string>();
            byte[] buffer = new byte[chunkSize];
            int chunkIndex = 0;

            using (FileStream sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                while (sourceStream.Position < sourceStream.Length)
                {
                    int bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length),cancellationToken);
                    string chunkPath = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(filePath)}_chunk{chunkIndex}{Path.GetExtension(filePath)}");
                    await File.WriteAllBytesAsync(chunkPath, buffer.AsMemory(0, bytesRead).ToArray(),cancellationToken);
                    chunkPaths.Add(chunkPath);
                    chunkIndex++;
                }
            }

            return chunkPaths;
        }
        /// <inheritdoc/>
        public async Task ConcatenateChunksAsync(List<string> chunkPaths, string outputFilePath, CancellationToken cancellationToken = default)
        {
            using (FileStream outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                foreach (string chunkPath in chunkPaths)
                {
                    byte[] chunkData = await File.ReadAllBytesAsync(chunkPath, cancellationToken);
                    await outputStream.WriteAsync(chunkData.AsMemory(0, chunkData.Length), cancellationToken);
                }
            }
        }

    }
}
