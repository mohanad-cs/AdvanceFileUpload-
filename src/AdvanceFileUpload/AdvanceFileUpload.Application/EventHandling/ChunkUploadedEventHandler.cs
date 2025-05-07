using AdvanceFileUpload.Application.Hubs;
using AdvanceFileUpload.Application.Response;
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
    /// Handles the <see cref="ChunkUploadedEvent"/> notification. and publishes integration events if enabled in the settings.<br/>
    /// </summary>
    public sealed class ChunkUploadedEventHandler : INotificationHandler<ChunkUploadedEvent>
    {
        private readonly IRepository<FileUploadSession> _fileUploadSessionRepository;
        private readonly IUploadProcessNotifier _uploadProcessNotifier;
        private readonly ILogger<ChunkUploadedEventHandler> _logger;
        private readonly IIntegrationEventPublisher _integrationEventPublisher;
        private readonly UploadSetting _uploadSetting;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkUploadedEventHandler"/> class.
        /// </summary>
        /// <param name="fileUploadSessionRepository">The repository for managing file upload sessions.</param>
        /// <param name="uploadProcessNotifier">The notifier for upload progress updates.</param>
        /// <param name="integrationEventPublisher">The publisher for integration events.</param>
        /// <param name="uploadSetting">The upload settings configuration.</param>
        /// <param name="logger">The logger for logging information and warnings.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the dependencies are null.</exception>
        public ChunkUploadedEventHandler(
            IRepository<FileUploadSession> fileUploadSessionRepository,
            IUploadProcessNotifier uploadProcessNotifier,
            IIntegrationEventPublisher integrationEventPublisher,
            IOptions<UploadSetting> uploadSetting,
            ILogger<ChunkUploadedEventHandler> logger)
        {
            _fileUploadSessionRepository = fileUploadSessionRepository ?? throw new ArgumentNullException(nameof(fileUploadSessionRepository));
            _uploadProcessNotifier = uploadProcessNotifier ?? throw new ArgumentNullException(nameof(uploadProcessNotifier));
            _integrationEventPublisher = integrationEventPublisher ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
            _uploadSetting = uploadSetting?.Value ?? throw new ArgumentNullException(nameof(uploadSetting));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the <see cref="ChunkUploadedEvent"/> notification.
        /// </summary>
        /// <param name="notification">The event notification containing details of the uploaded chunk.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the notification is null.</exception>
        public async Task Handle(ChunkUploadedEvent notification, CancellationToken cancellationToken)
        {
            if (notification is null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            var fileUploadSession = await _fileUploadSessionRepository.GetByIdAsync(notification.ChunkFile.SessionId, cancellationToken);
            if (fileUploadSession is null)
            {
                _logger.LogWarning("File upload session not found for session id {SessionId}.", notification.ChunkFile.SessionId);
                return;
            }

            UploadSessionStatusNotification uploadSessionStatusNotification = new UploadSessionStatusNotification
            {
                SessionId = fileUploadSession.Id,
                FileSize = fileUploadSession.FileSize,
                MaxChunkSize = fileUploadSession.MaxChunkSize,
                ProgressPercentage = fileUploadSession.ProgressPercentage,
                RemainChunks = fileUploadSession.GetRemainChunks(),
                SessionStartDate = fileUploadSession.SessionStartDate,
                SessionEndDate = fileUploadSession.SessionEndDate,
                TotalChunksToUpload = fileUploadSession.TotalChunksToUpload,
                TotalUploadedChunks = fileUploadSession.TotalUploadedChunks,
                UploadStatus = (UploadStatus)fileUploadSession.Status,
            };

            await _uploadProcessNotifier.NotifyUploadProgressAsync(fileUploadSession.CurrentHubConnectionId, uploadSessionStatusNotification);

            if (_uploadSetting.EnableIntegrationEventPublishing)
            {
                ChunkUploadedIntegrationEvent chunkUploadedIntegrationEvent = new ChunkUploadedIntegrationEvent
                {
                    SessionId = notification.ChunkFile.SessionId,
                    ChunkIndex = notification.ChunkFile.ChunkIndex,
                };

                PublishMessage<ChunkUploadedIntegrationEvent> publishMessage = new PublishMessage<ChunkUploadedIntegrationEvent>
                {
                    Message = chunkUploadedIntegrationEvent,
                    Queue = IntegrationConstants.ChunkUploadedConstants.Queue,
                    RoutingKey = IntegrationConstants.ChunkUploadedConstants.RoutingKey,
                    Exchange = IntegrationConstants.ChunkUploadedConstants.Exchange,
                    ExchangeType = IntegrationConstants.ChunkUploadedConstants.ExchangeType,
                    Durable = IntegrationConstants.ChunkUploadedConstants.Durable,
                    Exclusive = IntegrationConstants.ChunkUploadedConstants.Exclusive,
                    AutoDelete = IntegrationConstants.ChunkUploadedConstants.AutoDelete
                };

                _logger.LogInformation("Publishing ChunkUploadedIntegrationEvent for session {SessionId} and chunk index {ChunkIndex}.", chunkUploadedIntegrationEvent.SessionId, chunkUploadedIntegrationEvent.ChunkIndex);
                await _integrationEventPublisher.PublishAsync(publishMessage, cancellationToken);
            }
        }
    }
}
