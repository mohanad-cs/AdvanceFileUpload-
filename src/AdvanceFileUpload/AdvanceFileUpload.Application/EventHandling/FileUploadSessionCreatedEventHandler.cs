using AdvanceFileUpload.Application.Settings;
using AdvanceFileUpload.Domain.Events;
using AdvanceFileUpload.Integration;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdvanceFileUpload.Application.EventHandling
{
    /// <summary>
    /// Handles the <see cref="FileUploadSessionCreatedEvent"/> to publish integration events if enabled.
    /// </summary>
    public sealed class FileUploadSessionCreatedEventHandler : INotificationHandler<FileUploadSessionCreatedEvent>
    {
        private readonly UploadSetting _uploadSetting;
        private readonly IIntegrationEventPublisher _integrationEventPublisher;
        private readonly ILogger<FileUploadSessionCreatedEventHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadSessionCreatedEventHandler"/> class.
        /// </summary>
        /// <param name="uploadSetting">The upload settings.</param>
        /// <param name="integrationEventPublisher">The integration event publisher.</param>
        /// <param name="logger">The logger instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the parameters are null.</exception>
        public FileUploadSessionCreatedEventHandler(IOptions<UploadSetting> uploadSetting, IIntegrationEventPublisher integrationEventPublisher, ILogger<FileUploadSessionCreatedEventHandler> logger)
        {
            _integrationEventPublisher = integrationEventPublisher ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
            _uploadSetting = uploadSetting?.Value ?? throw new ArgumentNullException(nameof(uploadSetting));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the <see cref="FileUploadSessionCreatedEvent"/> by publishing an integration event if enabled in the settings.
        /// </summary>
        /// <param name="notification">The event notification containing the file upload session details.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the notification is null.</exception>
        public async Task Handle(FileUploadSessionCreatedEvent notification, CancellationToken cancellationToken)
        {
            if (notification is null)
            {
                throw new ArgumentNullException(nameof(notification));
            }
            _logger.LogInformation("Handling FileUploadSessionCreatedEvent for session {SessionId}", notification.FileUploadSession.Id);
            if (_uploadSetting.EnableIntegrationEventPublishing)
            {
                var sessionCreatedIntegrationEvent = new SessionCreatedIntegrationEvent()
                {
                    SessionId = notification.FileUploadSession.Id,
                    FileName = notification.FileUploadSession.FileName,
                    FileSize = notification.FileUploadSession.FileSize,
                    FileExtension = notification.FileUploadSession.FileExtension,
                    SessionStartDateTime = notification.FileUploadSession.SessionStartDate,
                };

                PublishMessage<SessionCreatedIntegrationEvent> publishMessage = new PublishMessage<SessionCreatedIntegrationEvent>()
                {
                    Message = sessionCreatedIntegrationEvent,
                    Queue = IntegrationConstants.SessionCreatedConstants.Queue,
                    RoutingKey = IntegrationConstants.SessionCreatedConstants.RoutingKey,
                    Exchange = IntegrationConstants.SessionCreatedConstants.Exchange,
                    ExchangeType = IntegrationConstants.SessionCreatedConstants.ExchangeType,
                    Durable = IntegrationConstants.SessionCreatedConstants.Durable,
                    Exclusive = IntegrationConstants.SessionCreatedConstants.Exclusive,
                    AutoDelete = IntegrationConstants.SessionCreatedConstants.AutoDelete
                };

                _logger.LogInformation("Publishing FileUploadSessionCreatedIntegrationEvent for session {SessionId}", notification.FileUploadSession.Id);
                await _integrationEventPublisher.PublishAsync(publishMessage, cancellationToken);
            }
        }
    }
}
