namespace AdvanceFileUpload.Domain
{
    /// <summary>
    ///   Specifies values that indicate whether a compression operation emphasizes speed
    ///     or compression size.
    /// </summary>
    public enum CompressionLevel
    {

        /// <summary>
        ///  The compression operation should optimally balance compression speed and output size.
        /// </summary>
        Optimal = 0,
        /// <summary>
        ///   The compression operation should complete as quickly as possible, even if the
        ///   resulting file is not optimally compressed.
        /// </summary>
        Fastest = 1,
        /// <summary>
        ///  No compression should be performed on the file.
        /// </summary>
        NoCompression = 2,
        /// <summary>
        /// The compression operation should create output as small as possible, even if
        /// the operation takes a longer time to complete.   
        /// </summary>
        SmallestSize = 3
    }
}
