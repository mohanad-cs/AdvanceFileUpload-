namespace AdvanceFileUpload.Client
{
    /// <summary>
    /// Provides data for the event that is raised when a file compression operation is completed.
    /// </summary>
    /// <remarks>This class contains information about the original file size and the compressed file size,
    /// which can be used to analyze the effectiveness of the compression process.</remarks>
    public class FileCompressionCompletedEventArg : EventArgs
    {
        /// <summary>
        /// Gets the original size of the file in bytes.
        /// </summary>
        public long OriginalFileSize { get; }
        /// <summary>
        /// Gets the size of the compressed file in bytes.
        /// </summary>
        public long CompressedFileSize { get; }
        /// <summary>
        /// Provides data for the event that is raised when a file compression operation is completed.
        /// </summary>
        /// <param name="originalFileSize">The size of the file, in bytes, before compression.</param>
        /// <param name="compressedFileSize">The size of the file, in bytes, after compression.</param>
        public FileCompressionCompletedEventArg(long originalFileSize, long compressedFileSize)
        {
            OriginalFileSize = originalFileSize;
            CompressedFileSize = compressedFileSize;
        }
    }
}
