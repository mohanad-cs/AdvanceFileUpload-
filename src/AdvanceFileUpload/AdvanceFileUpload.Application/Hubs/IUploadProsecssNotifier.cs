using AdvanceFileUpload.Application.Response;

namespace AdvanceFileUpload.Application.Hubs
{
    public interface IUploadProcessNotifier
    {
        Task NotifyUploadProgressAsync(string? connectionId, UploadSessionStatusNotification uploadSessionStatusNotification, CancellationToken cancellationToken = default);

    }
}
