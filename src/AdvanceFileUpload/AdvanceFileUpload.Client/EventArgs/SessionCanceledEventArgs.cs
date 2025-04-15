namespace AdvanceFileUpload.Client
{
    /// <summary>
    /// Provides data for the session canceled event.
    /// </summary>
    public class SessionCanceledEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the unique identifier for the session.
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// Gets the name of the file associated with the session.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets the size of the file associated with the session.
        /// </summary>
        public long FileSize { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionCanceledEventArgs"/> class.
        /// </summary>
        /// <param name="sessionId">The unique identifier for the session.</param>
        /// <param name="fileName">The name of the file associated with the session.</param>
        /// <param name="fileSize">The size of the file associated with the session.</param>
        public SessionCanceledEventArgs(Guid sessionId, string fileName, long fileSize)
        {
            SessionId = sessionId;
            FileName = fileName;
            FileSize = fileSize;
        }
    }
}
