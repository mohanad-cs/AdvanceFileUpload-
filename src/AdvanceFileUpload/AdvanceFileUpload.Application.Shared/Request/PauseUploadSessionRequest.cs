namespace AdvanceFileUpload.Application.Request
{
    /// <summary>
    /// Represents a request to pause an upload session.
    /// </summary>
    public sealed record PauseUploadSessionRequest
    {
        /// <summary>
        /// Gets the unique identifier of the upload session.
        /// </summary>
        public Guid SessionId { get; init; }
    }
}
