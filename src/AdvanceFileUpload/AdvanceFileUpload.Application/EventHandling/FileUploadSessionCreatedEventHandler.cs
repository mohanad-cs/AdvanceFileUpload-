using AdvanceFileUpload.Application.Settings;
using AdvanceFileUpload.Domain.Events;
using AdvanceFileUpload.Integration.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdvanceFileUpload.Application.EventHandling
{
    //TODO: Implement the functionality of publishing to RabbitMQ
    public sealed class FileUploadSessionCreatedEventHandler : INotificationHandler<FileUploadSessionCreatedEvent>
    {
        private readonly UploadSetting _uploadSetting;
        private readonly IIntegrationEventPublisher _integrationEventPublisher;
        private readonly ILogger<FileUploadSessionCreatedEventHandler> _logger;

        public FileUploadSessionCreatedEventHandler(IOptions<UploadSetting> uploadSetting, IIntegrationEventPublisher integrationEventPublisher, ILogger<FileUploadSessionCreatedEventHandler> logger)
        {
            _integrationEventPublisher = integrationEventPublisher ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
            _uploadSetting = uploadSetting?.Value ?? throw new ArgumentNullException(nameof(uploadSetting));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        ///<inheritdoc/>
        public async Task Handle(FileUploadSessionCreatedEvent notification, CancellationToken cancellationToken)
        {
            if (notification is null)
            {
                throw new ArgumentNullException(nameof(notification));
            }
            if (_uploadSetting.EnableIntegrationEventPublishing)
            {
                _logger.LogInformation("Publishing FileUploadSessionCreatedIntegrationEvent for session {SessionId}", notification.FileUploadSession.Id);
                await _integrationEventPublisher.PublishAsync(new SessionCreatedIntegrationEvent
                {
                    SessionId = notification.FileUploadSession.Id,
                    FileName = notification.FileUploadSession.FileName,
                    FileSize = notification.FileUploadSession.FileSize,
                    FileExtension = notification.FileUploadSession.FileExtension,
                    SessionStartDateTime = notification.FileUploadSession.SessionStartDate,
                   
                }, cancellationToken);
            }
        }
    }
}
