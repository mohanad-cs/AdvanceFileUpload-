namespace AdvanceFileUpload.Client
{
    /// <summary>
    /// Provides data for the session completed event.
    /// </summary>
    public class SessionCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the unique identifier for the session.
        /// </summary>
        public Guid SessionId { get; }

        /// <summary>
        /// Gets the name of the file that was uploaded.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets the size of the file that was uploaded.
        /// </summary>
        public long FileSize { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionCompletedEventArgs"/> class.
        /// </summary>
        /// <param name="sessionId">The unique identifier for the session.</param>
        /// <param name="fileName">The name of the file that was uploaded.</param>
        /// <param name="fileSize">The size of the file that was uploaded.</param>
        public SessionCompletedEventArgs(Guid sessionId, string fileName, long fileSize)
        {
            SessionId = sessionId;
            FileName = fileName;
            FileSize = fileSize;
        }
    }
}
