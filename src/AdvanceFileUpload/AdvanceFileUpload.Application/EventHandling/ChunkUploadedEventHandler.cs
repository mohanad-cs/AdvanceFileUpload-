using AdvanceFileUpload.Application.Hubs;
using AdvanceFileUpload.Application.Response;
using AdvanceFileUpload.Domain;
using AdvanceFileUpload.Domain.Core;
using AdvanceFileUpload.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AdvanceFileUpload.Application.EventHandling
{
    //TODO: Implement the functionality of publishing to RabbitMQ
    public sealed class ChunkUploadedEventHandler : INotificationHandler<ChunkUploadedEvent>
    {
        private readonly IRepository<FileUploadSession> _fileUploadSessionRepository;
        private readonly IUploadProcessNotifier _uploadProcessNotifier;
        private readonly ILogger<ChunkUploadedEventHandler> _logger;
        public ChunkUploadedEventHandler(IRepository<FileUploadSession> fileUploadSessionRepository,  IUploadProcessNotifier uploadProcessNotifier ,ILogger<ChunkUploadedEventHandler> logger)
        {
            _fileUploadSessionRepository = fileUploadSessionRepository ?? throw new ArgumentNullException(nameof(fileUploadSessionRepository));
            _uploadProcessNotifier = uploadProcessNotifier ?? throw new ArgumentNullException(nameof(uploadProcessNotifier));
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
          await  _uploadProcessNotifier.NotifyUploadProgressAsync(fileUploadSession.CurrentHubConnectionId, uploadSessionStatusNotification);

        }
    }
}
