using AdvanceFileUpload.Application.Response;
using Microsoft.AspNetCore.SignalR;

namespace AdvanceFileUpload.Application.Hubs
{
    public class UploadProcessHub : Hub
    {
        /// <summary>
        /// The name of the method that receives upload process notifications.
        /// </summary>
        public const string MethodName = "ReceiveUploadProcessNotification";
        public Task SendUploadProcessNotificationAsync(string connectionId, UploadSessionStatusNotification uploadSessionStatusNotification, CancellationToken cancellationToken)
        {
            return Clients.Client(connectionId).SendAsync(MethodName, uploadSessionStatusNotification, cancellationToken);

        }
    }
}
