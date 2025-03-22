using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Diagnostics;
namespace AdvanceFileUpload.Benchmark;

public class FileSplitterWithConcurrency
{
    private readonly ILogger _logger;

    public FileSplitterWithConcurrency(ILogger logger) => _logger = logger;

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
            FileOptions.Asynchronous | FileOptions.SequentialScan // Optimized flags
        );

        var writeTasks = new List<Task>();
        // Limit concurrent writes to 4 (adjust based on environment)
        var concurrencySemaphore = new SemaphoreSlim(4);
        int chunkIndex = 0;
        while (sourceStream.Position < sourceStream.Length)
        {
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

        _logger?.LogInformation("File split into {ChunkCount} chunks in {ElapsedMs} ms", chunkPaths.Count, stopwatch.ElapsedMilliseconds);
        return chunkPaths;
    }
   
}
