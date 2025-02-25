using AdvanceFileUpload.Application.Compression;
using System.Security.Cryptography;

namespace AdvanceFileUpload.Application.Validators
{
    /// <summary>
    /// Represents a chunk validator.
    /// </summary>
    public class ChunkValidator : IChunkValidator
    {
        ///<inheritdoc/>
        public bool IsValidChunkIndex(int chunkIndex)
        {
            return chunkIndex >= 0;
        }
        ///<inheritdoc/>
        public bool IsValidChunkData(byte[] chunkData, long MaxChunkSize)
        {
            return chunkData != null && chunkData.Length > 0 && chunkData.Length <= MaxChunkSize;
        }
        ///<inheritdoc/>
        public bool IsValidChunkSize(long chunkSize, long maxChunkSize)
        {
            return chunkSize > 0 && chunkSize <= maxChunkSize;
        }

        ///<inheritdoc/>
        public bool IsValidCompressedChunkData(byte[] chunkData, CompressionAlgorithmOption compressionAlgorithmOption)
        {
            if (chunkData == null || chunkData.Length < 2)
                return false;

            switch (compressionAlgorithmOption)
            {
                case CompressionAlgorithmOption.GZip:
                    return CheckGZip(chunkData);
                case CompressionAlgorithmOption.Deflate:
                    return CheckZLib(chunkData);
                case CompressionAlgorithmOption.Brotli:
                    return CheckBrotli(chunkData);
                default:
                    throw new ArgumentException("Unsupported compression algorithm.");
            }
        }

        private static bool CheckGZip(byte[] bytes)
        {
            // GZip header starts with 0x1F8B
            return bytes.Length >= 2 && bytes[0] == 0x1F && bytes[1] == 0x8B;
        }

        private static bool CheckZLib(byte[] bytes)
        {
            // ZLib header is 2 bytes. Check if (CMF*256 + FLG) is divisible by 31 and CM is 8 (Deflate)
            if (bytes.Length < 2)
                return false;

            ushort header = (ushort)((bytes[0] << 8) | bytes[1]);
            if (header % 31 != 0)
                return false;

            byte cmf = bytes[0];
            int cm = cmf >> 4; // Compression method (must be 8 for Deflate)
            int cinfo = cmf & 0x0F; // Window size (must be <=7)
            return cm == 8 && cinfo <= 7;
        }

        private static bool CheckBrotli(byte[] bytes)
        {
            // Brotli header starts with 0xCE 0xB2 0xCF 0x81
            return bytes.Length >= 4 &&
                   bytes[0] == 0xCE &&
                   bytes[1] == 0xB2 &&
                   bytes[2] == 0xCF &&
                   bytes[3] == 0x81;
        }
    }
}
