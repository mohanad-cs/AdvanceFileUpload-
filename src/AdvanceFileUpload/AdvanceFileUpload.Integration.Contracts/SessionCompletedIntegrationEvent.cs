namespace AdvanceFileUpload.Integration.Contracts
{
    /// <summary>
    /// Event triggered when a session is completed.
    /// </summary>
    public class SessionCompletedIntegrationEvent
    {
        /// <summary>
        /// Gets or sets the session identifier.
        /// </summary>
        public Guid SessionId { get; set; }

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the file extension.
        /// </summary>
        public string FileExtension { get; set; }

        /// <summary>
        /// Gets or sets the file size.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the session start date and time.
        /// </summary>
        public DateTime SessionStartDateTime { get; set; }

        /// <summary>
        /// Gets or sets the session end date and time.
        /// </summary>
        public DateTime SessionEndDateTime { get; set; }
    }
}
