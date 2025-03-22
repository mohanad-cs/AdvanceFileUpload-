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
    public sealed class FileUploadSessionCanceledEventHandler : INotificationHandler<FileUploadSessionCanceledEvent>
    {
        private readonly UploadSetting _uploadSetting;
        private readonly IIntegrationEventPublisher _integrationEventPublisher;
        private readonly ILogger<FileUploadSessionCreatedEventHandler> _logger;
        private readonly IRepository<FileUploadSession> _fileUploadSessionRepository;
        public FileUploadSessionCanceledEventHandler(IOptions<UploadSetting> uploadSetting, IIntegrationEventPublisher integrationEventPublisher, IRepository<FileUploadSession> fileUploadSessionRepository, ILogger<FileUploadSessionCreatedEventHandler> logger)
        {
          
            _uploadSetting = uploadSetting?.Value ?? throw new ArgumentNullException(nameof(uploadSetting));
            _integrationEventPublisher = integrationEventPublisher ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileUploadSessionRepository = fileUploadSessionRepository ?? throw new ArgumentNullException(nameof(fileUploadSessionRepository));
        }


        public async Task Handle(FileUploadSessionCanceledEvent notification, CancellationToken cancellationToken)
        {
           _logger.LogInformation("Handling FileUploadSessionCanceledEvent for session {SessionId}", notification.FileUploadSession.Id);
            FileUploadSession? canceledSession = await _fileUploadSessionRepository.GetByIdAsync(notification.FileUploadSession.Id, cancellationToken);
            if (canceledSession is null)
            {
                _logger.LogWarning("We cant handle cancelation of session because File upload session not found for session id {SessionId}.", notification.FileUploadSession.Id);
                return;
            }
            var chunksTobeDeleted = canceledSession.ChunkFiles.Select(x => x.ChunkPath).ToList();
            foreach (var chunk in chunksTobeDeleted)
            {
                _logger.LogInformation("Deleting chunk file {ChunkPath} for session {SessionId}", chunk, notification.FileUploadSession.Id);
                File.Delete(chunk);
            }
            if (_uploadSetting.EnableIntegrationEventPublishing)
            {
                _logger.LogInformation("Publishing FileUploadSessionCanceledIntegrationEvent for session {SessionId}", notification.FileUploadSession.Id);
                await _integrationEventPublisher.PublishAsync(new SessionCancelledIntegrationEvent
                {
                    SessionId = notification.FileUploadSession.Id,
                    FileName = notification.FileUploadSession.FileName,
                    FileSize = notification.FileUploadSession.FileSize,
                    FileExtension = notification.FileUploadSession.FileExtension,
                    SessionStartDateTime = notification.FileUploadSession.SessionStartDate,
                    SessionEndDateTime = (DateTime)notification.FileUploadSession.SessionEndDate,
            
                }, cancellationToken);
            }
        }
    }
}
