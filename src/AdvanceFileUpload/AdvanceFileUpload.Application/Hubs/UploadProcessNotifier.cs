using AdvanceFileUpload.Application.Response;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace AdvanceFileUpload.Application.Hubs
{
    /// <summary>
    /// Notifies clients about the progress of an upload session.
    /// </summary>
    public class UploadProcessNotifier : IUploadProcessNotifier
    {
        private readonly IHubContext<UploadProcessHub> _hubContext;
        private readonly ILogger<UploadProcessNotifier> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadProcessNotifier"/> class.
        /// </summary>
        /// <param name="hubContext">The SignalR hub context used to send notifications to clients.</param>
        /// <param name="logger">The logger used for logging information and warnings.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="hubContext"/> or <paramref name="logger"/> is null.</exception>
        public UploadProcessNotifier(IHubContext<UploadProcessHub> hubContext, ILogger<UploadProcessNotifier> logger)
        {
            _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task NotifyUploadProgressAsync(string? connectionId, UploadSessionStatusNotification uploadSessionStatusNotification, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
            {
                _logger.LogWarning("We can not send an upload process notification because the ConnectionId is null or empty.");
                await Task.CompletedTask;
            }
            else
            {
                _logger.LogInformation("Sending upload process notification to connection id {ConnectionId}.", connectionId);
                await _hubContext.Clients.Client(connectionId).SendAsync(UploadProcessHub.MethodName, uploadSessionStatusNotification, cancellationToken);
            }
        }
    }
}
