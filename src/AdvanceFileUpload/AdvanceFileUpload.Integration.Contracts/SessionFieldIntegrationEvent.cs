namespace AdvanceFileUpload.Integration.Contracts
{
    /// <summary>
    /// Event triggered when a session is Field.
    /// </summary>
    public class SessionFieldIntegrationEvent
    {
        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        public Guid SessionId { get; set; }
        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the file size.
        /// </summary>
        public long FileSize { get; set; }
        /// <summary>
        /// Gets or sets the file extension.
        /// </summary>
        public string FileExtension { get; set; } = string.Empty;
    }
}
