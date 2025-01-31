using AdvanceFileUpload.Domain.Core;

namespace AdvanceFileUpload.Domain.Events
{
    /// <summary>
    /// Event that is triggered when a chunk of a file is uploaded.
    /// </summary>
    public sealed class ChunkUploadedEvent : DomainEventBase
    {
        /// <summary>
        /// Gets the chunk file associated with this event.
        /// </summary>
        public ChunkFile ChunkFile { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkUploadedEvent"/> class.
        /// </summary>
        /// <param name="chunkFile">The chunk file that has been uploaded.</param>
        /// <exception cref="ArgumentNullException">Thrown when the chunk file is null.</exception>
        public ChunkUploadedEvent(ChunkFile chunkFile) : base()
        {
            ChunkFile = chunkFile ?? throw new ArgumentNullException(nameof(chunkFile));
        }
    }
}
