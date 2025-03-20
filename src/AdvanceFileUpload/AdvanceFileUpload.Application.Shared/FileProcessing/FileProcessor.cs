using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Diagnostics;

namespace AdvanceFileUpload.Application.FileProcessing
{
    /// <summary>
    /// Provides methods for file operations such as concatenating file chunks, saving files, and splitting files into chunks.
    /// </summary>
    public class FileProcessor : IFileProcessor
    {
        private readonly ILogger? _logger;

        public FileProcessor(ILogger? logger =null)
        {
            _logger = logger;
        }
        /// <inheritdoc/>
        public async Task SaveFileAsync(string fileName, byte[] fileData, string outputDirectory, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            Directory.CreateDirectory(outputDirectory);

            string filePath = Path.Combine(outputDirectory, fileName);
            await File.WriteAllBytesAsync(filePath, fileData, cancellationToken);

            stopwatch.Stop();
            _logger?.LogInformation("File saved at {Time} with size {Size} bytes in {ElapsedMilliseconds} ms", DateTime.Now, fileData.Length, stopwatch.ElapsedMilliseconds);
        }
        /// <inheritdoc/>
        //public async Task<List<string>> SplitFileIntoChunksAsync(string filePath, long chunkSize, string outputDirectory, CancellationToken cancellationToken = default)
        //{
        //    var stopwatch = Stopwatch.StartNew();

        //    if (!File.Exists(filePath))
        //    {
        //        throw new FileNotFoundException("The specified file does not exist.", filePath);
        //    }

        //    Directory.CreateDirectory(outputDirectory);

        //    List<string> chunkPaths = new List<string>();
        //    byte[] buffer = new byte[chunkSize];
        //    int chunkIndex = 0;

        //    using (FileStream sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
        //    {
        //        List<Task> writeTasks = new List<Task>();

        //        while (sourceStream.Position < sourceStream.Length)
        //        {
        //            int bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
        //            string chunkPath = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(filePath)}_chunk{chunkIndex}{Path.GetExtension(filePath)}");
        //            chunkPaths.Add(chunkPath);

        //            // Reuse the buffer and write asynchronously
        //            byte[] chunkData = new byte[bytesRead];
        //            Array.Copy(buffer, chunkData, bytesRead);
        //            writeTasks.Add(File.WriteAllBytesAsync(chunkPath, chunkData, cancellationToken));

        //            chunkIndex++;
        //        }

        //        await Task.WhenAll(writeTasks);
        //    }

        //    stopwatch.Stop();
        //    _logger?.LogInformation("File with size {size} split into {ChunkCount} chunks at {Time} in {ElapsedMilliseconds} ms", new FileInfo(filePath).Length, chunkPaths.Count, DateTime.Now, stopwatch.ElapsedMilliseconds);

        //    return chunkPaths;
        //}
        //public async Task<List<string>> SplitFileIntoChunksAsync(string filePath, long chunkSize, string outputDirectory, CancellationToken cancellationToken = default)
        //{
        //    var stopwatch = Stopwatch.StartNew();

        //    if (!File.Exists(filePath))
        //        throw new FileNotFoundException("The specified file does not exist.", filePath);

        //    Directory.CreateDirectory(outputDirectory);

        //    List<string> chunkPaths = new List<string>();
        //    using FileStream sourceStream = new FileStream(
        //        filePath,
        //        FileMode.Open,
        //        FileAccess.Read,
        //        FileShare.Read,
        //        bufferSize: 81920, // Larger buffer size
        //        FileOptions.Asynchronous | FileOptions.SequentialScan // Optimized flags
        //    );

        //    var writeTasks = new List<Task>();
        //    // Limit concurrent writes to 4 (adjust based on environment)
        //    var concurrencySemaphore = new SemaphoreSlim(4);
        //    int chunkIndex = 0;
        //    while (sourceStream.Position < sourceStream.Length)
        //    {
        //        // Rent buffer from pool (handle chunkSize > int.MaxValue if needed)
        //        byte[] buffer = ArrayPool<byte>.Shared.Rent((int)chunkSize);
        //        int bytesRead = await sourceStream.ReadAsync(buffer, 0, (int)chunkSize, cancellationToken);
        //        string chunkPath = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(filePath)}_chunk{chunkIndex++}{Path.GetExtension(filePath)}");
        //        chunkPaths.Add(chunkPath);

        //        // Acquire concurrency slot
        //        await concurrencySemaphore.WaitAsync(cancellationToken);

        //        writeTasks.Add(Task.Run(async () =>
        //        {
        //            try
        //            {
        //                using (FileStream chunkStream = new FileStream(
        //                    chunkPath,
        //                    FileMode.Create,
        //                    FileAccess.Write,
        //                    FileShare.None,
        //                    bufferSize: 81920,
        //                    FileOptions.Asynchronous))
        //                {
        //                    await chunkStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
        //                }
        //            }
        //            finally
        //            {
        //                ArrayPool<byte>.Shared.Return(buffer);
        //                concurrencySemaphore.Release();
        //            }
        //        }, cancellationToken));
        //    }

        //    await Task.WhenAll(writeTasks);
        //    stopwatch.Stop();

        //    _logger?.LogInformation("File split into {ChunkCount} chunks in {ElapsedMs} ms", chunkPaths.Count, stopwatch.ElapsedMilliseconds);
        //    return chunkPaths;
        //}
        public async Task<List<string>> SplitFileIntoChunksAsync(
     string filePath,
     long chunkSize,
     string outputDirectory,
     CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file does not exist.", filePath);

            Directory.CreateDirectory(outputDirectory);
            List<string> chunkPaths = new List<string>();

            using FileStream sourceStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920, // Larger buffer size
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            // List to hold asynchronous write tasks.
            var writeTasks = new List<Task>();

            // Limit concurrent writes to 4.
            using var concurrencySemaphore = new SemaphoreSlim(4);
            int chunkIndex = 0;
            while (sourceStream.Position < sourceStream.Length)
            {
                // Rent buffer from the shared pool.
                byte[] buffer = ArrayPool<byte>.Shared.Rent((int)chunkSize);
                int bytesRead = await sourceStream.ReadAsync(buffer, 0, (int)chunkSize, cancellationToken);

                // If no data is read, return the buffer and exit.
                if (bytesRead == 0)
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                    break;
                }

                string chunkPath = Path.Combine(
                    outputDirectory,
                    $"{Path.GetFileNameWithoutExtension(filePath)}_chunk{chunkIndex++}{Path.GetExtension(filePath)}");
                chunkPaths.Add(chunkPath);

                // Acquire a slot for writing.
                await concurrencySemaphore.WaitAsync(cancellationToken);

                // Schedule the write operation directly.
                writeTasks.Add(WriteChunkAsync(chunkPath, buffer, bytesRead, concurrencySemaphore, cancellationToken));
            }

            // Await all pending writes.
            await Task.WhenAll(writeTasks);
            stopwatch.Stop();
            _logger?.LogInformation("File split into {ChunkCount} chunks in {ElapsedMs} ms", chunkPaths.Count, stopwatch.ElapsedMilliseconds);
            return chunkPaths;
        }

        private async Task WriteChunkAsync(
            string chunkPath,
            byte[] buffer,
            int bytesRead,
            SemaphoreSlim semaphore,
            CancellationToken cancellationToken)
        {
            try
            {
                using FileStream chunkStream = new FileStream(
                    chunkPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    FileOptions.Asynchronous);
                await chunkStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            }
            finally
            {
                // Return the buffer to the pool and release the semaphore slot.
                ArrayPool<byte>.Shared.Return(buffer);
                semaphore.Release();
            }
        }
        /// <inheritdoc/>
        public async Task ConcatenateChunksAsync(List<string> chunkPaths, string outputFilePath, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            using (FileStream outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                foreach (string chunkPath in chunkPaths)
                {
                    byte[] chunkData = await File.ReadAllBytesAsync(chunkPath, cancellationToken);
                    await outputStream.WriteAsync(chunkData.AsMemory(0, chunkData.Length), cancellationToken);
                }
            }

            stopwatch.Stop();
            FileInfo fileInfo = new FileInfo(outputFilePath);
            _logger?.LogInformation("Chunks concatenated into file at {Time} with size {Size} bytes in {ElapsedMilliseconds} ms", DateTime.Now, fileInfo.Length, stopwatch.ElapsedMilliseconds);
        }
    }
}
