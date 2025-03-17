namespace AdvanceFileUpload.Application.Compression
{
    /// <summary>
    /// Specifies the compression algorithms available.
    /// </summary>
    public enum CompressionAlgorithmOption
    {

        /// <summary>
        /// GZip compression algorithm.
        /// Benefits: Good compression ratio and speed, widely supported.
        /// </summary>
        GZip,

        /// <summary>
        /// Deflate compression algorithm.
        /// Benefits: Fast compression and decompression, good for real-time applications.
        /// </summary>
        Deflate,

        /// <summary>
        /// Brotli compression algorithm.
        /// Benefits: High compression ratio, especially for web content, better than GZip in many cases.
        /// </summary>
        Brotli
    }
}
