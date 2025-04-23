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
        InProgress = 1,
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
        /// <remarks>Note that the session will be determinate as <see cref="Failed"/> if this conditions are met:
        /// <list type="number">
        /// <item>
        /// if the session is not <see cref="Completed"/> or <see cref="Canceled"/> . After 48h the session will be determinate as <see cref="Failed"/>.
        /// </item>
        /// <item>
        /// if the session status is <see cref="PendingToComplete"/> and the session has been waiting for 48h to be completed.
        /// </item>
        /// </list>
        /// </remarks>
        Failed,
    }
}
