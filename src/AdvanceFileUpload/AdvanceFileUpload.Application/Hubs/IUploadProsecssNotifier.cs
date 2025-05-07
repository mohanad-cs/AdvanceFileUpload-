using AdvanceFileUpload.Application.Response;

namespace AdvanceFileUpload.Application.Hubs
{

    /// <summary>
    /// Interface for notifying clients about the progress of an upload session.
    /// </summary>
    public interface IUploadProcessNotifier
    {
        /// <summary>
        /// Sends a notification to a specific client about the progress of an upload session.
        /// </summary>
        /// <param name="connectionId">The unique identifier of the client connection.</param>
        /// <param name="uploadSessionStatusNotification">The status notification of the upload session.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task NotifyUploadProgressAsync(string? connectionId, UploadSessionStatusNotification uploadSessionStatusNotification, CancellationToken cancellationToken = default);
    }
}
