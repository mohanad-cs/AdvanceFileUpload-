using Microsoft.Extensions.Logging;
namespace AdvanceFileUpload.Benchmark;


public class FileSplitterOriginal
{
    private readonly ILogger _logger;

    public FileSplitterOriginal(ILogger logger) => _logger = logger;

    public async Task<List<string>> SplitFileIntoChunksAsync(
        string filePath, long chunkSize, string outputDirectory, CancellationToken cancellationToken = default)
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
                int bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                string chunkPath = Path.Combine(outputDirectory, $"{Path.GetFileNameWithoutExtension(filePath)}_chunk{chunkIndex}{Path.GetExtension(filePath)}");
                await File.WriteAllBytesAsync(chunkPath, buffer.AsMemory(0, bytesRead).ToArray(), cancellationToken);
                chunkPaths.Add(chunkPath);
                chunkIndex++;
            }
        }

        return chunkPaths;
    }
}
