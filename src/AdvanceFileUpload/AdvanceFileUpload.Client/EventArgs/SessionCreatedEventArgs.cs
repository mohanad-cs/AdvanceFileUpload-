using AdvanceFileUpload.Application.Response;

namespace AdvanceFileUpload.Client
{
    public class SessionCreatedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the unique identifier of the upload session.
        /// </summary>
        public Guid SessionId { get; init; }

        /// <summary>
        /// Gets the size of the file to be uploaded.
        /// </summary>
        public long FileSize { get; init; }

        /// <summary>
        /// Gets the maximum size of each chunk.
        /// </summary>
        public long MaxChunkSize { get; init; }

        /// <summary>
        /// Gets the total number of chunks to be uploaded.
        /// </summary>
        public int TotalChunksToUpload { get; init; }

        /// <summary>
        /// Gets the current status of the upload session.
        /// </summary>
        public UploadStatus UploadStatus { get; init; }

        /// <summary>
        /// Gets the start date and time of the upload session.
        /// </summary>
        public DateTime SessionStartDate { get; init; }

        /// <summary>
        /// Creates a new instance of <see cref="SessionCreatedEventArgs"/> from the given <see cref="CreateUploadSessionResponse"/>.
        /// </summary>
        /// <param name="createUploadSessionResponse">The response containing the details of the created upload session.</param>
        /// <returns>A new instance of <see cref="SessionCreatedEventArgs"/>.</returns>
        public static SessionCreatedEventArgs Create(CreateUploadSessionResponse createUploadSessionResponse)
        {
            return new SessionCreatedEventArgs()
            {
                FileSize = createUploadSessionResponse.FileSize,
                MaxChunkSize = createUploadSessionResponse.MaxChunkSize,
                SessionId = createUploadSessionResponse.SessionId,
                SessionStartDate = createUploadSessionResponse.SessionStartDate,
                TotalChunksToUpload = createUploadSessionResponse.TotalChunksToUpload,
                UploadStatus = createUploadSessionResponse.UploadStatus
            };
        }
    }
}
