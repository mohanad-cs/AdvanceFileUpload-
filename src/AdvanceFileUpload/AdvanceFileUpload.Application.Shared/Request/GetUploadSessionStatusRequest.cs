namespace AdvanceFileUpload.Application.Request
{
    /// <summary>
    /// Represents a request to get the status of an upload session.
    /// </summary>
    public sealed record GetUploadSessionStatusRequest
    {
        /// <summary>
        /// Gets the unique identifier of the upload session.
        /// </summary>
        public Guid SessionId { get; init; }
    }
}
