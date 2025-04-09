namespace AdvanceFileUpload.Integration.Contracts
{
    /// <summary>
    /// Event triggered when a chunk is uploaded.
    /// </summary>
    public class ChunkUploadedIntegrationEvent
    {
        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        public Guid SessionId { get; set; }

        /// <summary>
        /// Gets or sets the chunk index.
        /// </summary>
        public int ChunkIndex { get; set; }
        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"ChunkUploadedIntegrationEvent {{ SessionId: {SessionId}, ChunkIndex:{ChunkIndex}}}";
        }
    }
}
