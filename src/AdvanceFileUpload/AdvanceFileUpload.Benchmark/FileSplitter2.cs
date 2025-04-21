namespace AdvanceFileUpload.Benchmark;

/// <summary>
/// Provides methods to split large files into chunks and concatenate chunks back into the original file.
/// Uses asynchronous I/O and parallel processing to maximize performance while optimizing memory usage.
/// </summary>
public static class FileSplitter2
{
    /// <summary>
    /// Splits the specified file into chunks of the provided size.
    /// The operation is performed using asynchronous reads and parallel writes.
    /// </summary>
    /// <param name="inputFile">Full path to the input file.</param>
    /// <param name="chunkSize">Size in bytes for each chunk.</param>
    /// <param name="outputDirectory">Directory where chunk files will be stored.</param>
    /// <returns>A list of full paths to the generated chunk files.</returns>
    public static async Task<List<string>> SplitFileAsync(string inputFile, long chunkSize, string outputDirectory)
    {
        if (!File.Exists(inputFile))
            throw new FileNotFoundException($"Input file not found: {inputFile}");

        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        var chunkFilePaths = new List<string>();
        var tasks = new List<Task>();
        int chunkIndex = 0;

        // SemaphoreSlim to throttle concurrent writes if needed (optional tuning)
        using var semaphore = new SemaphoreSlim(Environment.ProcessorCount);

        try
        {
            // Open input file for asynchronous sequential access.
            using FileStream inputStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            byte[] buffer = new byte[chunkSize];
            int bytesRead;

            // Read sequentially from the file, then write concurrently.
            while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
            {
                // Copy read bytes into new array if partial chunk is read (last chunk)
                byte[] chunkData = (bytesRead == chunkSize) ? buffer : buffer[..bytesRead];
                string chunkFileName = Path.Combine(outputDirectory, $"{Path.GetFileName(inputFile)}.part{chunkIndex:D4}");
                chunkFilePaths.Add(chunkFileName);

                // Wait for an available slot to write concurrently.
                await semaphore.WaitAsync().ConfigureAwait(false);

                // Initiate asynchronous write for the current chunk.
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await WriteChunkAsync(chunkFileName, chunkData).ConfigureAwait(false);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));

                chunkIndex++;
                // Refresh the buffer for the next read if using the full chunk size.
                if (bytesRead == chunkSize)
                {
                    buffer = new byte[chunkSize];
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error while splitting file: {ex.Message}", ex);
        }

        // Wait for any pending write operations to complete.
        await Task.WhenAll(tasks).ConfigureAwait(false);
        return chunkFilePaths;
    }

    /// <summary>
    /// Writes the given chunk data to a file asynchronously.
    /// </summary>
    /// <param name="chunkFilePath">Destination file path for the chunk.</param>
    /// <param name="data">Byte array containing chunk data.</param>
    private static async Task WriteChunkAsync(string chunkFilePath, byte[] data)
    {
        try
        {
            using FileStream chunkStream = new FileStream(chunkFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await chunkStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new IOException($"Error writing chunk to file: {chunkFilePath}", ex);
        }
    }

    /// <summary>
    /// Concatenates the specified chunk files into a single output file.
    /// Reads the chunk files in order and uses efficient buffering along with asynchronous I/O.
    /// </summary>
    /// <param name="chunkFiles">Ordered list of chunk file paths.</param>
    /// <param name="outputFile">Full path for the concatenated output file.</param>
    /// <returns>A task representing the asynchronous concatenation operation.</returns>
    public static async Task ConcatenateFileAsync(List<string> chunkFiles, string outputFile)
    {
        // Validate existence of all chunk files.
        foreach (var chunkFile in chunkFiles)
        {
            if (!File.Exists(chunkFile))
                throw new FileNotFoundException($"Missing chunk file: {chunkFile}");
        }

        try
        {
            using FileStream outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);

            // Process each chunk file in order, copying its content to the output stream.
            foreach (var chunkFile in chunkFiles.OrderBy(f => f))
            {
                using FileStream chunkStream = new FileStream(chunkFile, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
                await chunkStream.CopyToAsync(outputStream).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error during concatenation: {ex.Message}", ex);
        }
    }
}



