namespace AdvanceFileUpload.Client
{
    /// <summary>
    /// Defines the contract for a file upload service.
    /// </summary>
    public interface IFileUploadService : IDisposable
    {

        /// <summary>
        /// Uploads a file asynchronously.
        /// </summary>
        /// <param name="filePath">The path of the file to be uploaded.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task UploadFileAsync(string filePath);

        /// <summary>
        /// Pauses the ongoing file upload asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task PauseUploadAsync();

        /// <summary>
        /// Resumes the paused file upload asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task ResumeUploadAsync();

        /// <summary>
        /// Cancels the ongoing file upload asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CancelUploadAsync();

        /// <summary>
        /// Gets a value indicating whether the session can be paused.
        /// </summary>
        bool CanPauseSession { get; }

        /// <summary>
        /// Gets a value indicating whether the session can be canceled.
        /// </summary>
        bool CanCancelSession { get; }

        /// <summary>
        /// Gets a value indicating whether the session can be resumed.
        /// </summary>
        bool CanResumeSession { get; }

        /// <summary>
        /// Gets a value indicating whether the session is paused.
        /// </summary>
        bool IsSessionPaused { get; }

        /// <summary>
        /// Gets a value indicating whether the session is canceled.
        /// </summary>
        bool IsSessionCanceled { get; }

        /// <summary>
        /// Gets a value indicating whether the session is completed.
        /// </summary>
        bool IsSessionCompleted { get; }
        /// <summary>
        /// Get the list of file extensions that will be ignored during compression.
        /// </summary>
        IReadOnlyList<string> CompressionIgnoredExtensions { get;}
    }
}
