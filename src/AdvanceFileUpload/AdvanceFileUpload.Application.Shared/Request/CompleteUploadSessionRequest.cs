namespace AdvanceFileUpload.Application.Request
{
    /// <summary>
    /// Represents a request to complete a file upload session.
    /// </summary>
    public sealed record CompleteUploadSessionRequest
    {
        /// <summary>
        /// Gets the unique identifier of the upload session.
        /// </summary>
        public Guid SessionId { get; init; }
    }
}
