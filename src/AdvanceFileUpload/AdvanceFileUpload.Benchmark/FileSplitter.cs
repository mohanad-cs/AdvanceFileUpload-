using System.Buffers;
using System.Diagnostics;
namespace AdvanceFileUpload.Benchmark;
public class FileSplitter
{
    private const int DefaultBufferSize = 4096 * 16; // 64KB optimized for modern SSDs

    /// <summary>
    /// Splits a large file into chunks using parallel processing and async I/O
    /// </summary>
    public async Task<List<string>> SplitAsync(string inputPath, string outputDirectory, int chunkSizeInBytes, CancellationToken cancellationToken = default)
    {
        // Validate inputs
        if (!File.Exists(inputPath))
            throw new FileNotFoundException("Input file not found.", inputPath);
        if (chunkSizeInBytes <= 0)
            throw new ArgumentException("Chunk size must be positive.", nameof(chunkSizeInBytes));

        Directory.CreateDirectory(outputDirectory);

        var fileInfo = new FileInfo(inputPath);
        long fileSize = fileInfo.Length;
        string originalFileName = Path.GetFileName(inputPath);
        List<string> chunkPaths = new();

        using var inputStream = new FileStream(
            inputPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            DefaultBufferSize,
            FileOptions.Asynchronous | FileOptions.RandomAccess);

        int totalChunks = CalculateTotalChunks(fileSize, chunkSizeInBytes);
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = DetermineOptimalParallelism(),
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(
            Enumerable.Range(0, totalChunks),
            options,
            async (chunkIndex, ct) =>
            {
                long offset = (long)chunkIndex * chunkSizeInBytes;
                int bytesToRead = (int)Math.Min(chunkSizeInBytes, fileSize - offset);

                // Use pooled memory to minimize allocations
                byte[] buffer = ArrayPool<byte>.Shared.Rent(bytesToRead);
                try
                {
                    // Thread-safe read using explicit offset
                    int bytesRead = await inputStream.ReadAsync(
                        buffer.AsMemory(0, bytesToRead),
                        ct
                    ).ConfigureAwait(false);

                    if (bytesRead != bytesToRead)
                        throw new IOException($"Failed to read chunk {chunkIndex} (expected {bytesToRead} bytes, got {bytesRead})");

                    string chunkPath = GetChunkPath(outputDirectory, originalFileName, chunkIndex);
                    await WriteChunkAsync(chunkPath, buffer, bytesRead, ct).ConfigureAwait(false);
                    chunkPaths.Add(chunkPath);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            });
        return chunkPaths;
    }

    /// <summary>
    /// Concatenates chunk files back into the original file using buffered async I/O
    /// </summary>
    public async Task ConcatenateAsync(IEnumerable<string> chunkPaths, string outputFilePath, CancellationToken cancellationToken = default)
    {
        var sortedChunks = chunkPaths;


        using var outputStream = new FileStream(
            outputFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            DefaultBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        foreach (var chunkPath in sortedChunks)
        {
            await using var chunkStream = new FileStream(
                chunkPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                DefaultBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            // Use pooled buffer for efficient memory usage
            byte[] buffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
            try
            {
                int bytesRead;
                while ((bytesRead = await chunkStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    #region Helper Methods

    private static int CalculateTotalChunks(long fileSize, int chunkSize)
        => (int)((fileSize + chunkSize - 1) / chunkSize);

    private static int DetermineOptimalParallelism()
        => Math.Min(Environment.ProcessorCount * 2, 16); // Balance between I/O and CPU

    private static string GetChunkPath(string directory, string baseName, int index)
        => Path.Combine(directory, $"{baseName}.part{index:D5}");

    private static IEnumerable<string> GetSortedChunks(IEnumerable<string> chunks)
        => chunks.OrderBy(p =>
        {
            string numberPart = Path.GetExtension(p)[4..];
            return int.Parse(numberPart);
        });

    private async Task WriteChunkAsync(string path, byte[] buffer, int bytesToWrite, CancellationToken ct)
    {
        await using var chunkStream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            DefaultBufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        await chunkStream.WriteAsync(buffer.AsMemory(0, bytesToWrite), ct).ConfigureAwait(false);
    }

    private static void ValidateChunkSequence(IList<string> chunks)
    {
        if (!chunks.Any())
            throw new ArgumentException("No chunk files provided.");

        for (int i = 0; i < chunks.Count; i++)
        {
            string expectedSuffix = $".part{i:D5}";
            if (!chunks[i].EndsWith(expectedSuffix))
                throw new InvalidOperationException($"Missing or out-of-order chunk: Expected {expectedSuffix}");
        }
    }

    #endregion

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
        // _logger?.LogInformation("Chunks concatenated into file at {Time} with size {Size} bytes in {ElapsedMilliseconds} ms", DateTime.Now, fileInfo.Length, stopwatch.ElapsedMilliseconds);
    }
}

