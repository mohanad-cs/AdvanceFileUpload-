namespace AdvanceFileUpload.Application.Response
{
    /// <summary>
    /// Represents the status response of an upload session.
    /// </summary>
    public sealed record UploadSessionStatusResponse
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
        public long MaxMaxChunkSize { get; init; }

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
        /// Gets the total number of chunks that have been uploaded.
        /// </summary>
        public int TotalUploadedChunks { get; init; }

        /// <summary>
        /// Gets the progress percentage of the file upload.
        /// </summary>
        public double ProgressPercentage { get; init; }

        /// <summary>
        /// Gets the end date and time of the upload session, if it has ended.
        /// </summary>
        public DateTime? SessionEndDate { get; init; }

        /// <summary>
        /// Gets the list of remaining chunks to be uploaded.
        /// </summary>
        public List<int>? RemainChunks { get; init; }
    }
}
