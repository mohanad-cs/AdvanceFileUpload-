namespace AdvanceFileUpload.Domain
{
    /// <summary>
    /// Represents a chunk file in a file upload session.
    /// </summary>
    public sealed class ChunkFile
    {
        /// <summary>
        /// Gets the session identifier to which this chunk belongs.
        /// </summary>
        public Guid SessionId { get; private set; }

        /// <summary>
        /// Gets the index of the chunk.
        /// </summary>
        public int ChunkIndex { get; private set; }

        /// <summary>
        /// Gets the path of the chunk file.
        /// </summary>
        public string ChunkPath { get; private set; }

        /// <summary>
        /// Gets the size of the chunk file.
        /// </summary>
        public long ChunkSize { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkFile"/> class.
        /// </summary>
        /// <param name="sessionId">The session identifier to which this chunk belongs.</param>
        /// <param name="chunkIndex">The index of the chunk.</param>
        /// <param name="chunkPath">The path of the chunk file.</param>
        /// <exception cref="ArgumentException">Thrown when any of the parameters are invalid.</exception>
        internal ChunkFile(Guid sessionId, int chunkIndex, string chunkPath)
        {
            if (sessionId == Guid.Empty)
            {
                throw new ArgumentException("The session identifier must be specified.", nameof(sessionId));
            }
            if (chunkIndex < 0)
            {
                throw new ArgumentException("The Chunk Index must be >=0.", nameof(chunkIndex));
            }
            if (string.IsNullOrWhiteSpace(chunkPath))
            {
                throw new ArgumentException("The chunk path must be specified.", nameof(chunkPath));
            }
            if (!Path.Exists(chunkPath))
            {
                throw new ArgumentException("The chunk path does not exist.", nameof(chunkPath));
            }
            SessionId = sessionId;
            ChunkIndex = chunkIndex;
            ChunkPath = chunkPath;
            ChunkSize = new FileInfo(chunkPath).Length;
        }
    }
}
