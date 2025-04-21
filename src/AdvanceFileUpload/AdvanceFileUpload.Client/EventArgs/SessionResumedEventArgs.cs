namespace AdvanceFileUpload.Client
{
    /// <summary>
    /// Provides data for the session resumed event.
    /// </summary>
    public class SessionResumedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the unique identifier for the session.
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// Gets the name of the file being uploaded.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets the size of the file being uploaded.
        /// </summary>
        public long FileSize { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionResumedEventArgs"/> class.
        /// </summary>
        /// <param name="sessionId">The unique identifier for the session.</param>
        /// <param name="fileName">The name of the file being uploaded.</param>
        /// <param name="fileSize">The size of the file being uploaded.</param>
        public SessionResumedEventArgs(Guid sessionId, string fileName, long fileSize)
        {
            SessionId = sessionId;
            FileName = fileName;
            FileSize = fileSize;
        }
    }
}
