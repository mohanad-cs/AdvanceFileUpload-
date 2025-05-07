using AdvanceFileUpload.Application.Settings;
using AdvanceFileUpload.Domain;
using AdvanceFileUpload.Domain.Core;
using AdvanceFileUpload.Domain.Events;
using AdvanceFileUpload.Integration;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdvanceFileUpload.Application.EventHandling
{
    /// <summary>
    /// Handles the <see cref="FileUploadSessionCanceledEvent"/> to perform necessary actions when a file upload session is canceled.
    /// </summary>
    public sealed class FileUploadSessionCanceledEventHandler : INotificationHandler<FileUploadSessionCanceledEvent>
    {
        private readonly UploadSetting _uploadSetting;
        private readonly IIntegrationEventPublisher _integrationEventPublisher;
        private readonly ILogger<FileUploadSessionCreatedEventHandler> _logger;
        private readonly IRepository<FileUploadSession> _fileUploadSessionRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadSessionCanceledEventHandler"/> class.
        /// </summary>
        /// <param name="uploadSetting">The upload settings.</param>
        /// <param name="integrationEventPublisher">The integration event publisher.</param>
        /// <param name="fileUploadSessionRepository">The repository for file upload sessions.</param>
        /// <param name="logger">The logger instance.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the parameters are null.</exception>
        public FileUploadSessionCanceledEventHandler(
            IOptions<UploadSetting> uploadSetting,
            IIntegrationEventPublisher integrationEventPublisher,
            IRepository<FileUploadSession> fileUploadSessionRepository,
            ILogger<FileUploadSessionCreatedEventHandler> logger)
        {
            _uploadSetting = uploadSetting?.Value ?? throw new ArgumentNullException(nameof(uploadSetting));
            _integrationEventPublisher = integrationEventPublisher ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileUploadSessionRepository = fileUploadSessionRepository ?? throw new ArgumentNullException(nameof(fileUploadSessionRepository));
        }

        /// <summary>
        /// Handles the <see cref="FileUploadSessionCanceledEvent"/> by deleting associated chunk files and publishing an integration event if enabled.
        /// </summary>
        /// <param name="notification">The event notification containing the file upload session details.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task Handle(FileUploadSessionCanceledEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling FileUploadSessionCanceledEvent for session {SessionId}", notification.FileUploadSession.Id);

            // Retrieve the canceled session from the repository
            FileUploadSession? canceledSession = await _fileUploadSessionRepository.GetByIdAsync(notification.FileUploadSession.Id, cancellationToken);
            if (canceledSession is null)
            {
                _logger.LogWarning("We can't handle cancellation of session because File upload session not found for session id {SessionId}.", notification.FileUploadSession.Id);
                return;
            }

            // Delete associated chunk files
            var chunksTobeDeleted = canceledSession.ChunkFiles.Select(x => x.ChunkPath).ToList();
            foreach (var chunk in chunksTobeDeleted)
            {
                _logger.LogInformation("Deleting chunk file {ChunkPath} for session {SessionId}", chunk, notification.FileUploadSession.Id);
                File.Delete(chunk);
            }

            // Publish integration event if enabled
            if (_uploadSetting.EnableIntegrationEventPublishing)
            {
                var sessionCancelledIntegrationEvent = new SessionCancelledIntegrationEvent
                {
                    SessionId = notification.FileUploadSession.Id,
                    FileName = notification.FileUploadSession.FileName,
                    FileSize = notification.FileUploadSession.FileSize,
                    FileExtension = notification.FileUploadSession.FileExtension,
                    SessionStartDateTime = notification.FileUploadSession.SessionStartDate,
                    SessionEndDateTime = notification.FileUploadSession.SessionEndDate,
                };

                PublishMessage<SessionCancelledIntegrationEvent> publishMessage = new PublishMessage<SessionCancelledIntegrationEvent>
                {
                    Message = sessionCancelledIntegrationEvent,
                    Queue = IntegrationConstants.SessionCanceledConstants.Queue,
                    RoutingKey = IntegrationConstants.SessionCanceledConstants.RoutingKey,
                    Exchange = IntegrationConstants.SessionCanceledConstants.Exchange,
                    ExchangeType = IntegrationConstants.SessionCanceledConstants.ExchangeType,
                    Durable = IntegrationConstants.SessionCanceledConstants.Durable,
                    Exclusive = IntegrationConstants.SessionCanceledConstants.Exclusive,
                    AutoDelete = IntegrationConstants.SessionCanceledConstants.AutoDelete
                };
                _logger.LogInformation("Publishing FileUploadSessionCanceledIntegrationEvent for session {SessionId}", notification.FileUploadSession.Id);
                await _integrationEventPublisher.PublishAsync(publishMessage, cancellationToken);
            }
        }
    }
}
