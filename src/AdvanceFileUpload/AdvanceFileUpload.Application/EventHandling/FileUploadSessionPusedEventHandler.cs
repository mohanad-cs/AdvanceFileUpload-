using AdvanceFileUpload.Application.Settings;
using AdvanceFileUpload.Domain.Events;
using AdvanceFileUpload.Integration.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdvanceFileUpload.Application.EventHandling
{
    //TODO: Implement the functionality of publishing to RabbitMQ
    public sealed class FileUploadSessionPusedEventHandler : INotificationHandler<FileUploadSessionPausedEvent>
    {
        private readonly UploadSetting _uploadSetting;
        private readonly IIntegrationEventPublisher _integrationEventPublisher;
        private readonly ILogger<FileUploadSessionCreatedEventHandler> _logger;
        public FileUploadSessionPusedEventHandler(IOptions<UploadSetting> uploadSetting, IIntegrationEventPublisher integrationEventPublisher, ILogger<FileUploadSessionCreatedEventHandler> logger)
        {
            _uploadSetting = uploadSetting?.Value ?? throw new ArgumentNullException(nameof(uploadSetting));
            _integrationEventPublisher = integrationEventPublisher ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task Handle(FileUploadSessionPausedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling FileUploadSessionPausedEvent for session {SessionId}", notification.FileUploadSession.Id);
            if (_uploadSetting.EnableIntegrationEventPublishing)
            {
               var sessionPausedIntegrationEvent= new SessionPausedIntegrationEvent()
                {
                    SessionId = notification.FileUploadSession.Id,
                    FileName = notification.FileUploadSession.FileName,
                    FileSize = notification.FileUploadSession.FileSize,
                    FileExtension = notification.FileUploadSession.FileExtension,
                    SessionStartDateTime = notification.FileUploadSession.SessionStartDate,
                    SessionEndDateTime = (DateTime)notification.FileUploadSession.SessionEndDate,
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
