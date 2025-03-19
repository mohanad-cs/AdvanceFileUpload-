namespace AdvanceFileUpload.Application.Request
{
    /// <summary>
    /// Represents a request to cancel an upload session.
    /// </summary>
    public sealed record CancelUploadSessionRequest
    {
        /// <summary>
        /// Gets the unique identifier of the upload session.
        /// </summary>
        public Guid SessionId { get; init; }
    }
}
