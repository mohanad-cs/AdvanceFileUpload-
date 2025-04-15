using System.Buffers;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
namespace AdvanceFileUpload.Benchmark;

public class FileSplitterOptimized
{
    private readonly ILogger _logger;

    public FileSplitterOptimized(ILogger logger) => _logger = logger;

    public async Task<List<string>> SplitFileIntoChunksAsync(
        string filePath, long chunkSize, string outputDirectory, CancellationToken cancellationToken = default)
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
        string chunkPath, byte[] buffer, int bytesRead, SemaphoreSlim semaphore, CancellationToken cancellationToken)
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
}