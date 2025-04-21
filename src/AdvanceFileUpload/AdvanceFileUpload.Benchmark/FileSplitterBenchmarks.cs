using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
namespace AdvanceFileUpload.Benchmark;
[MemoryDiagnoser]
public class FileSplitterBenchmarks
{
    private string _testFilePath;
    private string _outputDirectory;
    private List<string> _chunkPaths;
    private readonly ILogger _logger = NullLogger.Instance;

    // Parameters: File size (100MB) and chunk size (10MB)
    [Params(1024 * 1024 * 300)]
    public long FileSize;

    [Params(1024 * 1024 * 5)]
    public long ChunkSize;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Create a test file with random data
        //_testFilePath = Path.GetTempFileName();
        //GenerateTestFile(_testFilePath, FileSize);
        _chunkPaths = Directory.GetFiles("D:\\Temp\\T").ToList();

    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        // Clean up the test file
        File.Delete(_testFilePath);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Create a fresh output directory for each iteration
        _outputDirectory = "D:\\Temp\\TT";
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        // Clean up the output directory after each iteration
        Directory.Delete(_outputDirectory, recursive: true);
    }

    //[Benchmark]
    //public async Task OriginalMethod()
    //{
    //    var splitter = new FileSplitterOriginal(_logger);
    //    await splitter.SplitFileIntoChunksAsync(_testFilePath, ChunkSize, _outputDirectory);
    //}

    //[Benchmark]
    //public async Task ConcurrencyLimitedMethod()
    //{
    //    var splitter = new FileSplitterWithConcurrency(_logger);
    //    await splitter.SplitFileIntoChunksAsync(_testFilePath, ChunkSize, _outputDirectory);
    //}

    //[Benchmark]
    //public async Task OptimizedMethod()
    //{
    //    var splitter = new FileSplitterOptimized(_logger);
    //    await splitter.SplitFileIntoChunksAsync(_testFilePath, ChunkSize, _outputDirectory);
    //}
    //[Benchmark]
    //public async Task NewOptimizedMethod()
    //{
    //    var splitter = new FileSplitter();
    //    await splitter.SplitAsync(_testFilePath,  _outputDirectory, (int)ChunkSize);
    //}
    //[Benchmark]
    //public async Task StaticSplitterMethod2()
    //{

    //    await FileSplitter2.SplitFileAsync(_testFilePath, ChunkSize,_outputDirectory);
    //}
    //[Benchmark]
    //public async Task StaticSplitterMethod3()
    //{

    //    await FileSplitter3.SplitFileAsync(_testFilePath, _outputDirectory, ChunkSize);
    //}
    //[Benchmark]
    //public async Task SplitterMethod4()
    //{
    //    var splitter = new FileSplitter4();
    //    await splitter.SplitFileAsync(_testFilePath, _outputDirectory, ChunkSize);
    //}
    //[Benchmark]
    //public async Task SplitterMethod5()
    //{
    //    var splitter = new FileSplitter5();
    //    await splitter.SplitFileAsync(_testFilePath, (int)ChunkSize);
    //}
    [Benchmark]
    public async Task FileMergin1()
    {
        var splitter = new FileSplitter();
        await splitter.ConcatenateAsync(_chunkPaths, _outputDirectory);
    }
    [Benchmark]
    public async Task FileMergin2()
    {
        var splitter = new FileSplitter();
        await splitter.ConcatenateChunksAsync(_chunkPaths, _outputDirectory);
    }

    private void GenerateTestFile(string path, long size)
    {
        using var fs = new FileStream(path, FileMode.Create);
        var random = new Random();
        byte[] buffer = new byte[81920];
        long bytesRemaining = size;

        while (bytesRemaining > 0)
        {
            int bytesToWrite = (int)Math.Min(buffer.Length, bytesRemaining);
            random.NextBytes(buffer.AsSpan(0, bytesToWrite));
            fs.Write(buffer, 0, bytesToWrite);
            bytesRemaining -= bytesToWrite;
        }
    }
}
