using AdvanceFileUpload.Application.Request;
using AdvanceFileUpload.Application.Compression;
using System.Text;
using System.Xml.Linq;

namespace AdvanceFileUpload.Client.Test
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var uploadOptions = new UploadOptions
            {
                TempDirectory = "C:\\Temp",
                MaxConcurrentUploads = 2,
                MaxRetriesCount = 3,
                CompressionOption = new CompressionOption() { Algorithm = CompressionAlgorithmOption.GZip, Level = CompressionLevelOption.Optimal }
            };

            var service = new FileUploadService(new Uri("http://localhost:5021"), uploadOptions);

            // Subscribe to events if needed
            service.SessionCreated += (sender, e) =>
            {
                Console.WriteLine($"Session created with ID: {e.SessionId}, Total chunks To Upload: {e.TotalChunksToUpload}");
            };
            service.ChunkUploaded += (sender, e) =>
            {
                Console.WriteLine($"Chunk {e.ChunkIndex} uploaded. Size: {e.ChunkSize}");
            };
            service.SessionCompleted += (sender, e) =>
            {
                Console.WriteLine($"Session {e.SessionId} completed for file {e.FileName}");
            };
            service.UploadProgressChanged += (sender, e) =>
            {
                Console.WriteLine($"Progress: {e.ProgressPercentage}% , chunks uploaded {e.TotalUploadedChunks} of {e.TotalChunksToUpload}");
                Console.WriteLine($"Remaining chunks is {e.RemainChunks}");
            };
            await File.WriteAllTextAsync("D:\\Temp\\testfile.txt", GenerateRandomText(1024 * 1024 * 100));

            // Upload the file
            await service.UploadFileAsync("D:\\Temp\\testfile.txt");

            // Optionally pause, resume, or cancel
             await service.PauseUploadAsync();
            // await service.ResumeUploadAsync();
            // await service.CancelUploadAsync();

            // Dispose of the service when done
            service.Dispose();
            Console.ReadLine();
        }
        static string GenerateRandomText(int size)
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
    }

   

}
