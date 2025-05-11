using System.Buffers;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace AdvanceFileUpload.Application.FileProcessing
{
    /// <summary>
    /// Provides methods for file operations such as concatenating file chunks, saving files, and splitting files into chunks.
    /// </summary>
    public class FileProcessor : IFileProcessor
    {
        private readonly ILogger<FileProcessor> _logger;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        public FileProcessor(ILogger<FileProcessor> logger)
        {
            _logger = logger;
        }
        /// <inheritdoc/>
        //public async Task SaveFileAsync(string fileName, byte[] fileData, string outputDirectory, CancellationToken cancellationToken = default)
        //{
        //    var stopwatch = Stopwatch.StartNew();

        //    Directory.CreateDirectory(outputDirectory);

        //    string filePath = Path.Combine(outputDirectory, fileName);
        //    await File.WriteAllBytesAsync(filePath, fileData, cancellationToken);

        //    stopwatch.Stop();
        //    _logger?.LogInformation("File saved at {Time} with size {Size} bytes in {ElapsedMilliseconds} ms", DateTime.Now, fileData.Length, stopwatch.ElapsedMilliseconds);
        //}
        public async Task SaveFileAsync(string fileName, byte[] fileData, string outputDirectory,
    CancellationToken cancellationToken = default)
        {
            const int BufferSize = 81920; // 80 KB buffer (optimal for most storage devices)
            int LargeFileThreshold = Environment.Is64BitProcess ? 1024 * 1024 * 10 : 1024 * 1024 * 1; // 1 MB threshold for chunked writing

            var stopwatch = Stopwatch.StartNew();
            string filePath = Path.Combine(outputDirectory, fileName);

            // Create directory structure if needed
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath!);
            }

            await using (var fileStream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                if (fileData.Length <= LargeFileThreshold)
                {
                    // Direct write for small files
                    await fileStream.WriteAsync(fileData, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    // Chunked write for large files using array pooling
                    byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
                    try
                    {
                        int bytesWritten = 0;
                        while (bytesWritten < fileData.Length)
                        {
                            int chunkSize = Math.Min(BufferSize, fileData.Length - bytesWritten);
                            Array.Copy(fileData, bytesWritten, buffer, 0, chunkSize);

                            await fileStream.WriteAsync(buffer.AsMemory(0, chunkSize), cancellationToken)
                                .ConfigureAwait(false);

                            bytesWritten += chunkSize;
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }
            }

            stopwatch.Stop();

            _logger.LogInformation("File saved successfully: {FileName} | Size: {Size} bytes | Path: {Path} | Duration: {Duration}ms",
                fileName,
                fileData.Length,
                outputDirectory,
                stopwatch.ElapsedMilliseconds);
        }
        /// <inheritdoc/>
        public async Task<List<string>> SplitFileIntoChunksAsync(string filePath, long chunkSize, string outputDirectory, CancellationToken cancellationToken = default)
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
                FileOptions.Asynchronous | FileOptions.SequentialScan // Optimized flags
            );

            var writeTasks = new List<Task>();
            // Limit concurrent writes to 4 (adjust based on environment)
            var concurrencySemaphore = new SemaphoreSlim(4);
            int chunkIndex = 0;
            while (sourceStream.Position < sourceStream.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Rent buffer from pool (handle chunkSize > int.MaxValue if needed)
                byte[] buffer = ArrayPool<byte>.Shared.Rent((int)chunkSize);
                int bytesRead = await sourceStream.ReadAsync(buffer, 0, (int)chunkSize, cancellationToken);
                string chunkPath = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(filePath)}_chunk{chunkIndex++}{Path.GetExtension(filePath)}");
                chunkPaths.Add(chunkPath);

                // Acquire concurrency slot
                await concurrencySemaphore.WaitAsync(cancellationToken);

                writeTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        using (FileStream chunkStream = new FileStream(
                            chunkPath,
                            FileMode.Create,
                            FileAccess.Write,
                            FileShare.None,
                            bufferSize: 81920,
                            FileOptions.Asynchronous))
                        {
                            await chunkStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                        concurrencySemaphore.Release();
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(writeTasks);
            stopwatch.Stop();

            _logger.LogInformation("File split into {ChunkCount} chunks in {ElapsedMs} ms", chunkPaths.Count, stopwatch.ElapsedMilliseconds);
            return chunkPaths;
        }
        /// <inheritdoc/>
        //public async Task MergeChunksAsync(List<string> chunkPaths, string outputFilePath, CancellationToken cancellationToken = default)
        //{
        //    var stopwatch = Stopwatch.StartNew();

        //    using (FileStream outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        //    {
        //        foreach (string chunkPath in chunkPaths)
        //        {
        //            byte[] chunkData = await File.ReadAllBytesAsync(chunkPath, cancellationToken);
        //            await outputStream.WriteAsync(chunkData.AsMemory(0, chunkData.Length), cancellationToken);
        //        }
        //    }

        //    stopwatch.Stop();
        //    FileInfo fileInfo = new FileInfo(outputFilePath);
        //    _logger?.LogInformation("Chunks concatenated into file at {Time} with size {Size} bytes in {ElapsedMilliseconds} ms", DateTime.Now, fileInfo.Length, stopwatch.ElapsedMilliseconds);
        //}
        public async Task MergeChunksAsync(List<string> chunkPaths, string outputFilePath, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            const int bufferSize = 81920; // 80 KB buffer (optimized for large files)

            // Rent a buffer from the shared ArrayPool to reduce memory allocations.
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                // Create the output stream with optimal options for asynchronous writing.
                using (var outputStream = new FileStream(
                    outputFilePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize,
                    FileOptions.Asynchronous))
                {
                    // Process each chunk file.
                    foreach (string chunkPath in chunkPaths)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // Validate that the chunk file exists.
                        if (!File.Exists(chunkPath))
                        {
                            throw new FileNotFoundException($"Chunk file not found: {chunkPath}", chunkPath);
                        }

                        // Open the chunk file for asynchronous reading with sequential scan hint.
                        using (var inputStream = new FileStream(
                            chunkPath,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.Read,
                            bufferSize,
                            FileOptions.Asynchronous | FileOptions.SequentialScan))
                        {
                            int bytesRead;
                            // Read from the chunk file in blocks until the end of the stream.
                            while ((bytesRead = await inputStream.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken)
                                                               .ConfigureAwait(false)) > 0)
                            {
                                await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken)
                                                  .ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
            finally
            {
                // Always return the rented buffer to the pool.
                ArrayPool<byte>.Shared.Return(buffer);
            }

            stopwatch.Stop();
            var fileInfo = new FileInfo(outputFilePath);
            _logger.LogInformation("Merging of ({chunksCount}) Chunks  completed at {Time} | Size: {Size} bytes | Duration: {ElapsedMilliseconds} ms",
                chunkPaths.Count,DateTime.UtcNow, fileInfo.Length, stopwatch.ElapsedMilliseconds);
        }
    }
}

