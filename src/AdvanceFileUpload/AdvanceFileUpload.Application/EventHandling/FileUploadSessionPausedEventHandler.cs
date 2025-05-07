using AdvanceFileUpload.Application.Settings;
using AdvanceFileUpload.Domain.Events;
using AdvanceFileUpload.Integration;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdvanceFileUpload.Application.EventHandling
{

    /// <summary>
    /// Handles the <see cref="FileUploadSessionPausedEvent"/> to perform necessary actions when a file upload session is paused.
    /// </summary>

    public sealed class FileUploadSessionPausedEventHandler : INotificationHandler<FileUploadSessionPausedEvent>
    {
        private readonly UploadSetting _uploadSetting;
        private readonly IIntegrationEventPublisher _integrationEventPublisher;
        private readonly ILogger<FileUploadSessionCreatedEventHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadSessionPausedEventHandler"/> class.
        /// </summary>
        /// <param name="uploadSetting">The upload settings configuration.</param>
        /// <param name="integrationEventPublisher">The integration event publisher for publishing events.</param>
        /// <param name="logger">The logger instance for logging information.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the required dependencies are null.</exception>
        public FileUploadSessionPausedEventHandler(IOptions<UploadSetting> uploadSetting, IIntegrationEventPublisher integrationEventPublisher, ILogger<FileUploadSessionCreatedEventHandler> logger)
        {
            _uploadSetting = uploadSetting?.Value ?? throw new ArgumentNullException(nameof(uploadSetting));
            _integrationEventPublisher = integrationEventPublisher ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the <see cref="FileUploadSessionPausedEvent"/> by logging the event and publishing an integration event if enabled.
        /// </summary>
        /// <param name="notification">The event notification containing details of the paused file upload session.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task Handle(FileUploadSessionPausedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling FileUploadSessionPausedEvent for session {SessionId}", notification.FileUploadSession.Id);

            if (_uploadSetting.EnableIntegrationEventPublishing)
            {
                var sessionPausedIntegrationEvent = new SessionPausedIntegrationEvent()
                {
                    SessionId = notification.FileUploadSession.Id,
                    FileName = notification.FileUploadSession.FileName,
                    FileSize = notification.FileUploadSession.FileSize,
                    FileExtension = notification.FileUploadSession.FileExtension,
                    SessionStartDateTime = notification.FileUploadSession.SessionStartDate,
                    SessionEndDateTime = notification.FileUploadSession.SessionEndDate,
                };

                PublishMessage<SessionPausedIntegrationEvent> publishMessage = new PublishMessage<SessionPausedIntegrationEvent>()
                {
                    Message = sessionPausedIntegrationEvent,
                    Queue = IntegrationConstants.SessionPausedConstants.Queue,
                    RoutingKey = IntegrationConstants.SessionPausedConstants.RoutingKey,
                    Exchange = IntegrationConstants.SessionPausedConstants.Exchange,
                    ExchangeType = IntegrationConstants.SessionPausedConstants.ExchangeType,
                    Durable = IntegrationConstants.SessionPausedConstants.Durable,
                    Exclusive = IntegrationConstants.SessionPausedConstants.Exclusive,
                    AutoDelete = IntegrationConstants.SessionPausedConstants.AutoDelete
                };

                _logger.LogInformation("Publishing FileUploadSessionPausedIntegrationEvent for session {SessionId}", notification.FileUploadSession.Id);
                await _integrationEventPublisher.PublishAsync(publishMessage, cancellationToken);
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
