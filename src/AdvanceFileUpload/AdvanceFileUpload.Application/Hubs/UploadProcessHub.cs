using AdvanceFileUpload.Application.Response;
using Microsoft.AspNetCore.SignalR;

namespace AdvanceFileUpload.Application.Hubs
{
    public class UploadProcessHub : Hub
    {
        public Task SendUploadProcessNotificationAsync(string connectionId, UploadSessionStatusNotification uploadSessionStatusNotification, CancellationToken cancellationToken)
        {
            return Clients.Client(connectionId).SendAsync("ReceiveUploadProcessNotification", uploadSessionStatusNotification, cancellationToken);

        }
    }
}
