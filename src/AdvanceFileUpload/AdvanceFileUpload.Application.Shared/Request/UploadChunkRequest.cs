namespace AdvanceFileUpload.Application.Request
{
    /// <summary>
    /// Represents a request to upload a chunk of a file.
    /// </summary>
    public sealed class UploadChunkRequest
    {
        /// <summary>
        /// Gets the unique identifier of the upload session.
        /// </summary>
        public Guid SessionId { get; init; }

        /// <summary>
        /// Gets the index of the chunk being uploaded.
        /// </summary>
        public int ChunkIndex { get; init; }

        /// <summary>
        /// Gets the data of the chunk being uploaded.
        /// </summary>
        public required byte[] ChunkData { get; init; }
    }
}
