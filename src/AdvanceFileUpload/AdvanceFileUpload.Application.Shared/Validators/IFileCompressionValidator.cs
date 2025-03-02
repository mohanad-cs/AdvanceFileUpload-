using AdvanceFileUpload.Application.Compression;

namespace AdvanceFileUpload.Application.Validators
{
    /// <summary>
    /// Interface for validating compressed file data.
    /// </summary>
    public interface IFileCompressionValidator
    {
        /// <summary>
        /// Validates if the provided file data is correctly compressed using the specified compression algorithm.
        /// </summary>
        /// <param name="data">The file data to validate.</param>
        /// <param name="compressionAlgorithmOption">The compression algorithm used to compress the data.</param>
        /// <returns>True if the data is valid compressed data; otherwise, false.</returns>
        bool IsValidCompressedData(byte[] data, CompressionAlgorithmOption compressionAlgorithmOption);
    }
}
