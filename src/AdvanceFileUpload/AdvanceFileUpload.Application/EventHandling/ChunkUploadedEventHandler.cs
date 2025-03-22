using AdvanceFileUpload.Application.Hubs;
using AdvanceFileUpload.Application.Response;
using AdvanceFileUpload.Application.Settings;
using AdvanceFileUpload.Domain;
using AdvanceFileUpload.Domain.Core;
using AdvanceFileUpload.Domain.Events;
using AdvanceFileUpload.Integration.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdvanceFileUpload.Application.EventHandling
{
    //TODO: Implement the functionality of publishing to RabbitMQ
    public sealed class ChunkUploadedEventHandler : INotificationHandler<ChunkUploadedEvent>
    {
        private readonly IRepository<FileUploadSession> _fileUploadSessionRepository;
        private readonly IUploadProcessNotifier _uploadProcessNotifier;
        private readonly ILogger<ChunkUploadedEventHandler> _logger;
        private readonly IIntegrationEventPublisher _integrationEventPublisher;
        private readonly UploadSetting _uploadSetting;
        public ChunkUploadedEventHandler(IRepository<FileUploadSession> fileUploadSessionRepository, IUploadProcessNotifier uploadProcessNotifier, IIntegrationEventPublisher integrationEventPublisher, IOptions<UploadSetting> uploadSetting, ILogger<ChunkUploadedEventHandler> logger)
        {
            _fileUploadSessionRepository = fileUploadSessionRepository ?? throw new ArgumentNullException(nameof(fileUploadSessionRepository));
            _uploadProcessNotifier = uploadProcessNotifier ?? throw new ArgumentNullException(nameof(uploadProcessNotifier));
            _integrationEventPublisher = integrationEventPublisher ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
            _uploadSetting = uploadSetting?.Value ?? throw new ArgumentNullException(nameof(uploadSetting));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
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
                _logger.LogInformation("Publishing ChunkUploadedIntegrationEvent for session {SessionId} and chunk index {ChunkIndex}.", chunkUploadedIntegrationEvent.SessionId, chunkUploadedIntegrationEvent.ChunkIndex);
                await _integrationEventPublisher.PublishAsync(chunkUploadedIntegrationEvent, cancellationToken);
            }
        }
    }
}
