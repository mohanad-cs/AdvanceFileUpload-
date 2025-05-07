using AdvanceFileUpload.Application.Settings;
using AdvanceFileUpload.Domain.Events;
using AdvanceFileUpload.Integration;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdvanceFileUpload.Application.EventHandling
{
    /// <summary>
    /// Handles the <see cref="FileUploadSessionFailedEvent"/> notification.
    /// </summary>
    public sealed class FileUploadSessionFieldEventHandler : INotificationHandler<FileUploadSessionFailedEvent>
    {
        private readonly UploadSetting _uploadSetting;
        private readonly IIntegrationEventPublisher _integrationEventPublisher;
        private readonly ILogger<FileUploadSessionCreatedEventHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadSessionFieldEventHandler"/> class.
        /// </summary>
        /// <param name="uploadSetting">The upload settings.</param>
        /// <param name="integrationEventPublisher">The integration event publisher.</param>
        /// <param name="logger">The logger instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the required dependencies are null.</exception>
        public FileUploadSessionFieldEventHandler(IOptions<UploadSetting> uploadSetting, IIntegrationEventPublisher integrationEventPublisher, ILogger<FileUploadSessionCreatedEventHandler> logger)
        {
            _uploadSetting = uploadSetting?.Value ?? throw new ArgumentNullException(nameof(uploadSetting));
            _integrationEventPublisher = integrationEventPublisher ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the <see cref="FileUploadSessionFailedEvent"/> by deleting chunk files and optionally publishing an integration event.
        /// </summary>
        /// <param name="notification">The event notification containing details of the failed file upload session.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task Handle(FileUploadSessionFailedEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling FileUploadSessionFieldEvent for session {SessionId}", notification.FileUploadSession.Id);

            foreach (var chunk in notification.FileUploadSession.ChunkFiles)
            {
                _logger.LogInformation("Deleting chunk file {ChunkPath} for session {SessionId}", chunk.ChunkPath, notification.FileUploadSession.Id);
                File.Delete(chunk.ChunkPath);
            }

            if (_uploadSetting.EnableIntegrationEventPublishing)
            {
                var sessionFieldIntegrationEvent = new SessionFieldIntegrationEvent()
                {
                    SessionId = notification.FileUploadSession.Id,
                    FileName = notification.FileUploadSession.FileName,
                    FileSize = notification.FileUploadSession.FileSize,
                    FileExtension = notification.FileUploadSession.FileExtension,
                };

                PublishMessage<SessionFieldIntegrationEvent> publishMessage = new PublishMessage<SessionFieldIntegrationEvent>()
                {
                    Message = sessionFieldIntegrationEvent,
                    Queue = IntegrationConstants.SessionFieldConstants.Queue,
                    RoutingKey = IntegrationConstants.SessionFieldConstants.RoutingKey,
                    Exchange = IntegrationConstants.SessionFieldConstants.Exchange,
                    ExchangeType = IntegrationConstants.SessionFieldConstants.ExchangeType,
                    Durable = IntegrationConstants.SessionFieldConstants.Durable,
                    Exclusive = IntegrationConstants.SessionFieldConstants.Exclusive,
                    AutoDelete = IntegrationConstants.SessionFieldConstants.AutoDelete
                };

                _logger.LogInformation("Publishing FileUploadSessionFieldIntegrationEvent for session {SessionId}", notification.FileUploadSession.Id);
                await _integrationEventPublisher.PublishAsync(publishMessage, cancellationToken);
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
