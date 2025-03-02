using AdvanceFileUpload.Application.Compression;

namespace AdvanceFileUpload.Application.Validators
{
    /// <summary>
    ///  Represents a File Compression validator.
    /// </summary>
    public class FileCompressionValidator : IFileCompressionValidator
    {
        ///<inheritdoc/>
        public bool IsValidCompressedData(byte[] data, CompressionAlgorithmOption compressionAlgorithmOption)
        {
            if (data == null || data.Length < 2)
                return false;

            switch (compressionAlgorithmOption)
            {
                case CompressionAlgorithmOption.GZip:
                    return CheckGZip(data);
                case CompressionAlgorithmOption.Deflate:
                    return CheckZLib(data);
                case CompressionAlgorithmOption.Brotli:
                    return CheckBrotli(data);
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
            if (cm != 8 || cinfo > 7)
                return false;

            byte flg = bytes[1];
            int fcheck = flg & 0x1F; // Check bits for header validation
            int fdict = (flg >> 5) & 0x01; // Dictionary flag
            int flevel = (flg >> 6) & 0x03; // Compression level

            // Reserved bits in FLG should be zero
            if ((flg & 0xE0) != (flevel << 6 | fdict << 5 | fcheck))
                return false;

            // If dictionary flag is set, ensure the dictionary ID is present
            if (fdict == 1)
            {
                if (bytes.Length < 6)
                    return false;

                // Dictionary ID is 4 bytes after the header
                uint dictId = (uint)((bytes[2] << 24) | (bytes[3] << 16) | (bytes[4] << 8) | bytes[5]);
                // Additional validation for dictId can be added here if needed
            }

            return true;
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
