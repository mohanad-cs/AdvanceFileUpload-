namespace AdvanceFileUpload.Client
{
    /// <summary>
    /// Provides data for the ChunkUploaded event.
    /// </summary>
    public class ChunkUploadedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the session ID associated with the uploaded chunk.
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// Gets the index of the uploaded chunk.
        /// </summary>
        public int ChunkIndex { get; }

        /// <summary>
        /// Gets the size of the uploaded chunk.
        /// </summary>
        public long ChunkSize { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkUploadedEventArgs"/> class.
        /// </summary>
        /// <param name="sessionId">The session ID associated with the uploaded chunk.</param>
        /// <param name="chunkIndex">The index of the uploaded chunk.</param>
        /// <param name="chunkSize">The size of the uploaded chunk.</param>
        public ChunkUploadedEventArgs(Guid sessionId, int chunkIndex, long chunkSize)
        {
            SessionId = sessionId;
            ChunkIndex = chunkIndex;
            ChunkSize = chunkSize;
        }
    }
}
