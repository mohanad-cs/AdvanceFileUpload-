using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Domain.Test
{
    internal static class TestsUtility
    {
        public const string _pdfTestFilePath = @"..\TestFiles\testFile.Pdf";
        public const string _tempDirectory = "..\\Temp\\";
        public static string _fileName = Path.GetFileName(_pdfTestFilePath);
        public static long _fileSize = new FileInfo(_pdfTestFilePath).Length;
        public static long _maxChunkSize = 256 * 1024; // 256 KB
        public static string _testChunkPath = Path.Combine(_tempDirectory, "chunk0.Pdf");
       
        public static void InsureTestDataCreated()
        {
            if (!File.Exists(_testChunkPath))
            {
                File.Copy(@"..\TestFiles\chunk0.Pdf", _testChunkPath);
            }
        }
        public static FileUploadSession GetValidAllChunkUploadedNotCompletedFileUploadSession()
        {
            FileUploadSession fileUploadSession = new FileUploadSession(_fileName, _tempDirectory, _fileSize, null, _maxChunkSize);
            for (int i = 0; i < fileUploadSession.TotalChunksToUpload; i++)
            {
                fileUploadSession.AddChunk(i, Path.Combine(_tempDirectory, "chunk0.chunk"));
            }
            return fileUploadSession;
        }
        public static FileUploadSession GetFileUploadSessionWithRemainingChunks()
        {
            FileUploadSession fileUploadSession = new FileUploadSession(_fileName, _tempDirectory, _fileSize, null, _maxChunkSize);
            for (int i = 0; i < fileUploadSession.TotalChunksToUpload - 1; i++)
            {
                fileUploadSession.AddChunk(i, Path.Combine(_tempDirectory, "chunk0.chunk"));
            }
            return fileUploadSession;
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
    }
}
