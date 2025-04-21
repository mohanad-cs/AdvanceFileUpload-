using System.Text;
using AdvanceFileUpload.Application.Compression;
using AdvanceFileUpload.Application.Request;

namespace AdvanceFileUpload.Client.Test
{
    internal class Program
    {
        private static FileUploadService _service;
        private static bool _isUploading;
        private static readonly object _consoleLock = new object();

        // Console layout constants
        private const int StatusLine = 3;
        private const int ProgressLine = 4;
        private const int EventsLine = 6;
        private const int InputLine = 20;

        static async Task Main(string[] args)
        {
            Console.CursorVisible = false;
            InitializeConsoleLayout();

            var uploadOptions = new UploadOptions
            {
                TempDirectory = "D:\\Temp\\T",
                MaxConcurrentUploads = 4,
                MaxRetriesCount = 3,
                CompressionOption = new CompressionOption()
                {
                    Algorithm = CompressionAlgorithmOption.GZip,
                    Level = CompressionLevelOption.Optimal
                },
                APIKey="secret"
                
            };

            _service = new FileUploadService(new Uri("http://localhost:5021"), uploadOptions);

            await File.WriteAllTextAsync("D:\\Temp\\testfile.txt", GenerateRandomText(1024 * 1024 * 100));

            ConfigureServiceEvents();

            // var uploadTask = _service.UploadFileAsync("D:\\Temp\\testfile.txt");
            var uploadTask = _service.UploadFileAsync(@"D:\Temp\D09_20230518080711.mp4");
            _isUploading = true;

            _ = Task.Run(HandleUserInput);

            try
            {
                await uploadTask;
            }
            catch (Exception ex)
            {
                WriteToStatusArea($"Upload failed: {ex.Message}");
            }
            finally
            {
                // _isUploading = false;
                //// _service.Dispose();
                // Console.CursorVisible = true;
                // Console.SetCursorPosition(0, InputLine);
                // Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        private static void InitializeConsoleLayout()
        {
            Console.Clear();

            Console.WriteLine("=== File Upload Service ===");
            Console.WriteLine("\nEvents:");
            DrawSeparator(EventsLine - 1);
            DrawSeparator(InputLine - 1);

            Console.SetCursorPosition(0, InputLine);
            Console.Write("COMMANDS [1:Pause, 2:Resume, 3:Cancel, 4:Status] > ");
        }

        private static void DrawSeparator(int line)
        {
            Console.SetCursorPosition(0, line);
            Console.WriteLine(new string('=', Console.WindowWidth));
        }

        private static void ConfigureServiceEvents()
        {

            _service.FileSplittingStarted += (sender, e) =>
            {
                WriteToEventsArea($"File Splitting Started");
            };
            _service.FileSplittingCompleted += (sender, e) =>
            {
                WriteToEventsArea($"File Splitting Completed");
            };
            _service.FileCompressionStarted += (sender, e) =>
            {
                WriteToEventsArea($"File Compression Started");
            };
            _service.FileCompressionCompleted += (sender, e) =>
            {
                WriteToEventsArea($"File Compression Completed");
            };
            _service.SessionPausing += (sender, e) =>
            {
                WriteToEventsArea($"File Upload Pausing");
            };
            _service.SessionResuming += (sender, e) =>
            {
                WriteToEventsArea($"File Upload Resuming");
            };
            _service.SessionCanceling += (sender, e) =>
            {
                WriteToEventsArea($"File Upload Canceling");
            };
            _service.SessionCreated += (sender, e) =>
            {
                WriteToEventsArea($"Session Created: {e.SessionId}, Chunks: {e.TotalChunksToUpload}");
            };

            _service.ChunkUploaded += (sender, e) =>
            {
                WriteToProgressArea($"Chunk {e.ChunkIndex} uploaded ({e.ChunkSize} bytes)");
            };

            _service.UploadProgressChanged += (sender, e) =>
            {
                UpdateProgressBar((int)e.ProgressPercentage);
            };

            _service.SessionPaused += (sender, e) =>
            {
                WriteToStatusArea("Upload PAUSED");
                WriteToEventsArea($"Session Paused: {e.SessionId}");
            };

            _service.SessionResumed += (sender, e) =>
            {
                WriteToStatusArea("Upload RESUMED");
                WriteToEventsArea($"Session Resumed: {e.SessionId}");
            };

            _service.SessionCompleted += (sender, e) =>
            {
                WriteToStatusArea("Upload COMPLETED");
                WriteToEventsArea($"Session Completed: {e.SessionId}");
            };
        }

        private static async Task HandleUserInput()
        {
            while (true)
            {
                var input = await ReadInputAsync();

                switch (input)
                {
                    case 1 when _isUploading:
                        await _service.PauseUploadAsync();
                        break;
                    case 2 when _service.CanResumeSession:
                        _isUploading = true;
                        await _service.ResumeUploadAsync();
                        break;
                    case 3 when _service.CanCancelSession:
                        await _service.CancelUploadAsync();
                        _isUploading = false;
                        WriteToEventsArea("Upload CANCELED");
                        break;
                    case 4:
                        ShowStatus();
                        break;
                    default:
                        WriteToEventsArea("Invalid command");
                        break;
                }
            }
        }

        private static async Task<int> ReadInputAsync()
        {
            Console.CursorVisible = true;
            Console.SetCursorPosition(48, InputLine);
            Console.Write(new string(' ', 10));
            Console.SetCursorPosition(48, InputLine);

            var input = Console.ReadKey(intercept: true);
            Console.Write(input.KeyChar);

            Console.CursorVisible = false;
            return char.IsDigit(input.KeyChar) ? int.Parse(input.KeyChar.ToString()) : 0;
        }

        private static void UpdateProgressBar(int percentage)
        {
            lock (_consoleLock)
            {
                Console.SetCursorPosition(0, ProgressLine);
                var progress = $"{percentage}%".PadRight(5);
                var bar = new string('▓', percentage / 2) + new string('░', 50 - percentage / 2);
                Console.Write($"Progress: [{bar}] {progress}");
            }
        }

        private static void WriteToStatusArea(string message)
        {
            lock (_consoleLock)
            {
                Console.SetCursorPosition(0, StatusLine);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, StatusLine);
                Console.Write($"Status: {message}");
            }
        }

        private static void WriteToProgressArea(string message)
        {
            lock (_consoleLock)
            {
                Console.SetCursorPosition(0, ProgressLine + 1);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, ProgressLine + 1);
                Console.Write(message);
            }
        }

        private static void WriteToEventsArea(string message)
        {
            lock (_consoleLock)
            {
                Console.SetCursorPosition(0, EventsLine);
                Console.WriteLine(message.PadRight(Console.WindowWidth));
                if (Console.CursorTop >= InputLine - 1)
                {
                    Console.WriteLine();
                    Console.SetCursorPosition(0, EventsLine);
                }
            }
        }

        private static void ShowStatus()
        {
            WriteToEventsArea($"Current Status: {(_isUploading ? "Active" : "Inactive")}");
            WriteToEventsArea($"Can Pause: {_service.CanPauseSession}");
            WriteToEventsArea($"Can Resume: {_service.CanResumeSession}");
            WriteToEventsArea($"Can Cancel: {_service.CanCancelSession}");
        }

        static string GenerateRandomText(int size)
        {
            var characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var randomText = new StringBuilder(size);
            var random = new Random();

            for (int i = 0; i < size; i++)
            {
                randomText.Append(characters[random.Next(characters.Length)]);
            }

            return randomText.ToString();
        }
    }
}