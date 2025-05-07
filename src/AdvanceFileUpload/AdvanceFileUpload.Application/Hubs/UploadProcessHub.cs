using AdvanceFileUpload.Application.Response;
using Microsoft.AspNetCore.SignalR;

namespace AdvanceFileUpload.Application.Hubs
{

    /// <summary>
    /// Represents a SignalR hub for managing upload process notifications.
    /// </summary>
    public class UploadProcessHub : Hub
    {
        /// <summary>
        /// The name of the method used to send upload process notifications to clients.
        /// </summary>
        public const string MethodName = "ReceiveUploadProcessNotification";

        /// <summary>
        /// Sends an upload process notification to a specific client.
        /// </summary>
        /// <param name="connectionId">The connection ID of the client to send the notification to.</param>
        /// <param name="uploadSessionStatusNotification">The upload session status notification to send.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task SendUploadProcessNotificationAsync(string connectionId, UploadSessionStatusNotification uploadSessionStatusNotification, CancellationToken cancellationToken = default)
        {
            return Clients.Client(connectionId).SendAsync(MethodName, uploadSessionStatusNotification, cancellationToken);
        }
    }
}
