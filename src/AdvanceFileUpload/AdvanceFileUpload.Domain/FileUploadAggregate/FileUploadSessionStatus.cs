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
        InProgress=1,
        /// <summary>
        /// The file upload session is paused.
        /// </summary>
        Paused,
        /// <summary>
        ///  The file upload session is Watling to be Completed.
        /// </summary>
        PendingToComplete,
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
