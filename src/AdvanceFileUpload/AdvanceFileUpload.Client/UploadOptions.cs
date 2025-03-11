using AdvanceFileUpload.Application.Request;
using System.Reflection;

namespace AdvanceFileUpload.Client
{
    public class UploadOptions
    {
        public CompressionOption? CompressionOption { get; set; }
        public string TempDirectory { get; set; } = Path.GetTempPath();

    }
}
