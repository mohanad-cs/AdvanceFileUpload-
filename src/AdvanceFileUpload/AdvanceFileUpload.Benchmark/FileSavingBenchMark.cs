using AdvanceFileUpload.Application.FileProcessing;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Benchmark
{
    [MemoryDiagnoser]
    public class FileSavingBenchMark
    {
        private FileProcessor? _fileProcessor;
        private readonly string _fileName = "testFile.txt";
        private readonly byte[] _fileData = Encoding.UTF8.GetBytes(GenerateRandomText(1024*1024*400));
        private readonly string _outputDirectory = Path.Combine(Path.GetTempPath(), "BenchmarkTest");
        private string _filePath = string.Empty;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _fileProcessor = new FileProcessor(NullLogger<FileProcessor>.Instance);
            Directory.CreateDirectory(_outputDirectory);
            _filePath = Path.Combine(_outputDirectory, _fileName);
        }

        [Benchmark]
        public async Task SaveFileUsingMicrosoftFileClass_WriteAllBytes()
        {
           await File.WriteAllBytesAsync(_filePath, _fileData);
        }

        [Benchmark]
        public async Task SaveFileUsingIFileProcessor()
        {
            await _fileProcessor.SaveFileAsync(_fileName, _fileData, _outputDirectory);
        }
        [Benchmark]
        public async Task SaveFileUsingNew()
        {
            await SaveFileAsync(_fileName, _fileData, _outputDirectory);
        }
        public static string GenerateRandomText(int size)
        {
            // Define the characters to use for generating random text
            string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+-=[]{};':\",./<>? ";

            // Create a StringBuilder to store the random text
            StringBuilder randomText = new StringBuilder(size);

            // Random number generator
            Random random = new Random();

            // Generate random text
            for (int i = 0; i < size; i++)
            {
                randomText.Append(characters[random.Next(characters.Length)]);
            }

            return randomText.ToString();
        }

        public async Task SaveFileAsync(string fileName, byte[] fileData, string outputDirectory,
    CancellationToken cancellationToken = default)
        {
            const int BufferSize = 81920; // 80 KB buffer (optimal for most storage devices)
            int largeFileThreshold = Environment.Is64BitProcess ? 1024 * 1024 * 10 : 1024 * 1024;

            string filePath = Path.Combine(outputDirectory, fileName);
            var directoryPath = Path.GetDirectoryName(filePath);

            // Safely create directory structure
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            await using (var fileStream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                BufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                if (fileData.Length <= largeFileThreshold)
                {
                    // Direct write for small files
                    await fileStream.WriteAsync(fileData, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    // Optimized chunked write using memory slicing
                    int bytesWritten = 0;
                    while (bytesWritten < fileData.Length)
                    {
                        int chunkSize = Math.Min(BufferSize, fileData.Length - bytesWritten);
                        await fileStream.WriteAsync(fileData.AsMemory(bytesWritten, chunkSize), cancellationToken)
                            .ConfigureAwait(false);

                        bytesWritten += chunkSize;
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
            }
        }
    }
}
