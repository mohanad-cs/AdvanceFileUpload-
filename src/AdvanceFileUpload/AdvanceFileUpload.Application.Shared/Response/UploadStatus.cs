namespace AdvanceFileUpload.Application.Response
{
    /// <summary>
    /// Represents the status of an upload session.
    /// </summary>
    public enum UploadStatus
    {
        /// <summary>
        /// The upload session is pending.
        /// </summary>
        Pending,

        /// <summary>
        /// The upload session is in progress.
        /// </summary>
        InProgress,

        /// <summary>
        /// The upload session is completed.
        /// </summary>
        Completed,

        /// <summary>
        /// The upload session has failed.
        /// </summary>
        Failed
    }
}
