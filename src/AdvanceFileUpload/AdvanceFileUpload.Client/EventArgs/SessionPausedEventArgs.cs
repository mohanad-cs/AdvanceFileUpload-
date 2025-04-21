namespace AdvanceFileUpload.Client
{
    /// <summary>
    /// Provides data for the session paused event.
    /// </summary>
    public class SessionPausedEventArgs : EventArgs
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
        /// Initializes a new instance of the <see cref="SessionPausedEventArgs"/> class.
        /// </summary>
        /// <param name="sessionId">The unique identifier for the session.</param>
        /// <param name="fileName">The name of the file being uploaded.</param>
        /// <param name="fileSize">The size of the file being uploaded.</param>
        public SessionPausedEventArgs(Guid sessionId, string fileName, long fileSize)
        {
            SessionId = sessionId;
            FileName = fileName;
            FileSize = fileSize;
        }
    }
}
