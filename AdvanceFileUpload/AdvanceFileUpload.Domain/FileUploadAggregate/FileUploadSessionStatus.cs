namespace AdvanceFileUpload.Domain
{
    /// <summary>
    /// Represents the status of a file upload session.
    /// </summary>
    public enum FileUploadSessionStatus
    {
        /// <summary>
        /// The file upload session is in progress.
        /// </summary>
        InProgress,

        /// <summary>
        /// The file upload session is paused.
        /// </summary>
        Paused,

        /// <summary>
        /// The file upload session is completed.
        /// </summary>
        Completed,

        /// <summary>
        /// The file upload session is canceled.
        /// </summary>
        Canceled,

        /// <summary>
        /// The file upload session has failed.
        /// </summary>
        Failed,
    }
}
