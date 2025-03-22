using System.Buffers;

namespace AdvanceFileUpload.Benchmark;

/// <summary>
/// Provides methods to split a large file into smaller chunks and reassemble it efficiently.
/// Utilizes parallel and asynchronous I/O to maximize performance and minimize memory usage.
/// </summary>
public static class FileSplitter3
{
    /// <summary>
    /// Splits a large file into smaller chunks of the specified size using parallel async operations.
    /// </summary>
    /// <param name="inputFilePath">Path to the input file.</param>
    /// <param name="outputDirectory">Directory where the chunks will be created.</param>
    /// <param name="chunkSize">Chunk size in bytes.</param>
    /// <param name="maxDegreeOfParallelism">Maximum parallel tasks to write chunks.</param>
    /// <returns>List of paths for the generated chunk files.</returns>
    public static async Task<List<string>> SplitFileAsync(
        string inputFilePath,
        string outputDirectory,
        long chunkSize,
        int maxDegreeOfParallelism = 4)
    {
        var chunkPaths = new List<string>();
        Directory.CreateDirectory(outputDirectory);

        // Use a semaphore to limit maximum parallel writes
        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        var writeTasks = new List<Task>();

        using FileStream inputStream = new(
            inputFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            65536,
            FileOptions.Asynchronous);

        int chunkIndex = 0;
        while (inputStream.Position < inputStream.Length)
        {
            long remainingBytes = inputStream.Length - inputStream.Position;
            int bytesToRead = (int)Math.Min(chunkSize, remainingBytes);

            // Rent a buffer from the shared pool to reduce allocations
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bytesToRead);

            int readBytes = await inputStream.ReadAsync(buffer, 0, bytesToRead);
            if (readBytes == 0) break;

            string chunkPath = Path.Combine(outputDirectory, $"chunk_{chunkIndex:D5}.part");
            chunkPaths.Add(chunkPath);

            await semaphore.WaitAsync().ConfigureAwait(false);

            // Capture local references to avoid closure issues in loop
            var localBuffer = buffer;
            var localPath = chunkPath;
            int localBytesRead = readBytes;

            // Enqueue a write task
            writeTasks.Add(Task.Run(async () =>
            {
                try
                {
                    using FileStream chunkStream = new(
                        localPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        65536,
                        FileOptions.Asynchronous);

                    await chunkStream.WriteAsync(localBuffer, 0, localBytesRead).ConfigureAwait(false);
                    await chunkStream.FlushAsync().ConfigureAwait(false);
                }
                finally
                {
                    // Return the buffer to the pool and release the semaphore
                    ArrayPool<byte>.Shared.Return(localBuffer);
                    semaphore.Release();
                }
            }));

            chunkIndex++;
        }

        // Wait for all chunk writes to complete
        await Task.WhenAll(writeTasks).ConfigureAwait(false);
        return chunkPaths;
    }

    /// <summary>
    /// Reassembles a set of chunk files back into the original file asynchronously.
    /// </summary>
    /// <param name="chunkPaths">List of chunk file paths in the correct order.</param>
    /// <param name="outputFilePath">Path to the merged output file.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task ConcatenateFilesAsync(
        IEnumerable<string> chunkPaths,
        string outputFilePath)
    {
        using FileStream outputStream = new(
            outputFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            65536,
            FileOptions.Asynchronous);

        // Use a small buffer to control memory usage during merges
        byte[] buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);

        try
        {
            foreach (var chunkPath in chunkPaths)
            {
                if (!File.Exists(chunkPath))
                    throw new FileNotFoundException($"Chunk file not found: {chunkPath}");

                using FileStream chunkStream = new(
                    chunkPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    65536,
                    FileOptions.Asynchronous);

                int bytesRead;
                while ((bytesRead = await chunkStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                {
                    await outputStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                }
            }

            await outputStream.FlushAsync().ConfigureAwait(false);
        }
        finally
        {
            // Return the buffer to the pool
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}

/// <summary>
/// Provides methods to split a large file into smaller chunks and reassemble it efficiently.
/// Utilizes parallel and asynchronous I/O to maximize performance and minimize memory usage.
/// </summary>
public class FileSplitter4
{
    /// <summary>
    /// Splits a large file into smaller chunks of the specified size using parallel async operations.
    /// </summary>
    /// <param name="inputFilePath">Path to the input file.</param>
    /// <param name="outputDirectory">Directory where the chunks will be created.</param>
    /// <param name="chunkSize">Chunk size in bytes.</param>
    /// <param name="maxDegreeOfParallelism">Maximum parallel tasks to write chunks.</param>
    /// <returns>List of paths for the generated chunk files.</returns>
    public async Task<List<string>> SplitFileAsync(
        string inputFilePath,
        string outputDirectory,
        long chunkSize,
        int maxDegreeOfParallelism = 4)
    {
        var chunkPaths = new List<string>();
        Directory.CreateDirectory(outputDirectory);

        // Use a semaphore to limit maximum parallel writes
        var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        var writeTasks = new List<Task>();

        using FileStream inputStream = new(
            inputFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            65536,
            FileOptions.Asynchronous);

        int chunkIndex = 0;
        while (inputStream.Position < inputStream.Length)
        {
            long remainingBytes = inputStream.Length - inputStream.Position;
            int bytesToRead = (int)Math.Min(chunkSize, remainingBytes);

            // Rent a buffer from the shared pool to reduce allocations
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bytesToRead);

            int readBytes = await inputStream.ReadAsync(buffer, 0, bytesToRead);
            if (readBytes == 0) break;

            string chunkPath = Path.Combine(outputDirectory, $"chunk_{chunkIndex:D5}.part");
            chunkPaths.Add(chunkPath);

            await semaphore.WaitAsync().ConfigureAwait(false);

            // Capture local references to avoid closure issues in loop
            var localBuffer = buffer;
            var localPath = chunkPath;
            int localBytesRead = readBytes;

            // Enqueue a write task
            writeTasks.Add(Task.Run(async () =>
            {
                try
                {
                    using FileStream chunkStream = new(
                        localPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        65536,
                        FileOptions.Asynchronous);

                    await chunkStream.WriteAsync(localBuffer, 0, localBytesRead).ConfigureAwait(false);
                    await chunkStream.FlushAsync().ConfigureAwait(false);
                }
                finally
                {
                    // Return the buffer to the pool and release the semaphore
                    ArrayPool<byte>.Shared.Return(localBuffer);
                    semaphore.Release();
                }
            }));

            chunkIndex++;
        }

        // Wait for all chunk writes to complete
        await Task.WhenAll(writeTasks).ConfigureAwait(false);
        return chunkPaths;
    }

    /// <summary>
    /// Reassembles a set of chunk files back into the original file asynchronously.
    /// </summary>
    /// <param name="chunkPaths">List of chunk file paths in the correct order.</param>
    /// <param name="outputFilePath">Path to the merged output file.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ConcatenateFilesAsync(
        IEnumerable<string> chunkPaths,
        string outputFilePath)
    {
        using FileStream outputStream = new(
            outputFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            65536,
            FileOptions.Asynchronous);

        // Use a small buffer to control memory usage during merges
        byte[] buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);

        try
        {
            foreach (var chunkPath in chunkPaths)
            {
                if (!File.Exists(chunkPath))
                    throw new FileNotFoundException($"Chunk file not found: {chunkPath}");

                using FileStream chunkStream = new(
                    chunkPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    65536,
                    FileOptions.Asynchronous);

                int bytesRead;
                while ((bytesRead = await chunkStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                {
                    await outputStream.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                }
            }

            await outputStream.FlushAsync().ConfigureAwait(false);
        }
        finally
        {
            // Return the buffer to the pool
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
    public class FileSplitter5
    {

        private const int DefaultBufferSize = 81920; // 80 KB

        public async Task<List<string>> SplitFileAsync(string sourceFilePath, int chunkSize)
        {
            var chunkPaths = new List<string>();
            byte[] buffer = new byte[DefaultBufferSize];
            int chunkIndex = 0;

            using (FileStream sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, useAsync: true))
            {
                long totalSize = sourceStream.Length;
                long remainingBytes = totalSize;

                List<Task> writeTasks = new List<Task>();

                while (remainingBytes > 0)
                {
                    string chunkPath = $"{sourceFilePath}.chunk{chunkIndex}";
                    chunkIndex++;

                    chunkPaths.Add(chunkPath);

                    int bytesRead = 0;
                    using (MemoryStream chunkStream = new MemoryStream(chunkSize))
                    {
                        while (bytesRead < chunkSize && remainingBytes > 0)
                        {
                            int bytesToRead = (int)Math.Min(buffer.Length, remainingBytes);
                            int read = await sourceStream.ReadAsync(buffer, 0, bytesToRead);
                            if (read == 0)
                                break;

                            await chunkStream.WriteAsync(buffer, 0, read);
                            bytesRead += read;
                            remainingBytes -= read;
                        }

                        writeTasks.Add(WriteChunkAsync(chunkPath, chunkStream.ToArray()));
                    }
                }

                await Task.WhenAll(writeTasks);
            }
            return chunkPaths;
        }

        public async Task ConcatenateFileAsync(List<string> chunkPaths, string destinationFilePath)
        {
            byte[] buffer = new byte[DefaultBufferSize];

            using (FileStream destinationStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, DefaultBufferSize, useAsync: true))
            {
                foreach (var chunkPath in chunkPaths.OrderBy(p => p))
                {
                    using (FileStream chunkStream = new FileStream(chunkPath, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, useAsync: true))
                    {
                        int bytesRead;
                        while ((bytesRead = await chunkStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await destinationStream.WriteAsync(buffer, 0, bytesRead);
                        }
                    }
                }
            }
        }

        private async Task WriteChunkAsync(string chunkPath, byte[] data)
        {
            using (FileStream chunkStream = new FileStream(chunkPath, FileMode.Create, FileAccess.Write, FileShare.None, DefaultBufferSize, useAsync: true))
            {
                await chunkStream.WriteAsync(data, 0, data.Length);
            }
        }
    }
}



