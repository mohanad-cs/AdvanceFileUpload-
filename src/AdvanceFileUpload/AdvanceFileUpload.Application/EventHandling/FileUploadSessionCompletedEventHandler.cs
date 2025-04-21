using AdvanceFileUpload.Application.Compression;
using AdvanceFileUpload.Application.FileProcessing;
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
    public sealed class FileUploadSessionCompletedEventHandler : INotificationHandler<FileUploadSessionCompletedEvent>
    {

        private readonly IRepository<FileUploadSession> _fileUploadSessionRepository;
        private readonly ILogger<FileUploadSessionCompletedEventHandler> _logger;
        private readonly IFileCompressor _fileCompressor;
        private readonly IFileProcessor _fileProcessor;
        private readonly UploadSetting _uploadSetting;
        private readonly IIntegrationEventPublisher _integrationEventPublisher;

        public FileUploadSessionCompletedEventHandler(IRepository<FileUploadSession> fileUploadSessionRepository, IFileProcessor fileProcessor, IFileCompressor fileCompressor, IOptions<UploadSetting> uploadSetting, IIntegrationEventPublisher integrationEventPublisher, ILogger<FileUploadSessionCompletedEventHandler> logger)
        {
            if (uploadSetting is null)
            {
                throw new ArgumentNullException(nameof(uploadSetting));
            }
            _fileUploadSessionRepository = fileUploadSessionRepository ?? throw new ArgumentNullException(nameof(fileUploadSessionRepository));
            _fileProcessor = fileProcessor ?? throw new ArgumentNullException(nameof(fileProcessor));
            _fileCompressor = fileCompressor ?? throw new ArgumentNullException(nameof(fileCompressor));
            _uploadSetting = uploadSetting.Value;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _integrationEventPublisher = integrationEventPublisher;
        }


        public async Task Handle(FileUploadSessionCompletedEvent notification, CancellationToken cancellationToken)
        {
            if (notification is null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            _logger.LogInformation("Handling FileUploadSessionCompletedEvent for session {SessionId}", notification.FileUploadSession.Id);
            var chunkPaths = notification.FileUploadSession.ChunkFiles.OrderBy(x => x.ChunkIndex).Select(x => x.ChunkPath).ToList();
            string fileNamePostFix = notification.FileUploadSession.UseCompression ? ".gz" : string.Empty;
            var outputFilePath = Path.Combine(notification.FileUploadSession.SavingDirectory, notification.FileUploadSession.FileName + fileNamePostFix);

            _logger.LogInformation("Concatenating chunks for session {SessionId}", notification.FileUploadSession.Id);
            await _fileProcessor.MergeChunksAsync(chunkPaths, outputFilePath, cancellationToken);

            foreach (var chunk in notification.FileUploadSession.ChunkFiles)
            {
                _logger.LogInformation("Deleting chunk file {ChunkPath} for session {SessionId}", chunk.ChunkPath, notification.FileUploadSession.Id);
                File.Delete(chunk.ChunkPath);
            }

            if (notification.FileUploadSession.UseCompression)
            {
                _logger.LogInformation("Decompressing file for session {SessionId}", notification.FileUploadSession.Id);
                var tempDecompressedFilePath = Path.Combine(_uploadSetting.TempDirectory, Path.GetFileNameWithoutExtension(outputFilePath));
                await _fileCompressor.DecompressFileAsync(outputFilePath, _uploadSetting.TempDirectory, (CompressionAlgorithmOption)notification.FileUploadSession.CompressionAlgorithm, cancellationToken);
                File.Move(tempDecompressedFilePath, outputFilePath.Replace(".gz", string.Empty), true);
                File.Delete(outputFilePath);
            }
            _logger.LogInformation("File Upload successfully Compleated for session {SessionId}", notification.FileUploadSession.Id);
            if (_uploadSetting.EnableIntegrationEventPublishing)
            {
                SessionCompletedIntegrationEvent sessionCompletedIntegrationEvent = new SessionCompletedIntegrationEvent()
                {
                    SessionId = notification.FileUploadSession.Id,
                    FileName = notification.FileUploadSession.FileName,
                    FileSize = notification.FileUploadSession.FileSize,
                    FileExtension = notification.FileUploadSession.FileExtension,
                    FilePath = notification.FileUploadSession.SavingDirectory,
                    SessionStartDateTime = notification.FileUploadSession.SessionStartDate,
                    SessionEndDateTime = (DateTime)notification.FileUploadSession.SessionEndDate,

                };
                PublishMessage<SessionCompletedIntegrationEvent> publishMessage = new PublishMessage<SessionCompletedIntegrationEvent>()
                {
                    Message = sessionCompletedIntegrationEvent,
                    Queue = IntegrationConstants.SessionCompletedConstants.Queue,
                    RoutingKey = IntegrationConstants.SessionCompletedConstants.RoutingKey,
                    Exchange = IntegrationConstants.SessionCompletedConstants.Exchange,
                    ExchangeType = IntegrationConstants.SessionCompletedConstants.ExchangeType,
                    Durable = IntegrationConstants.SessionCompletedConstants.Durable,
                    Exclusive = IntegrationConstants.SessionCompletedConstants.Exclusive,
                    AutoDelete = IntegrationConstants.SessionCompletedConstants.AutoDelete
                };
                _logger.LogInformation("Publishing SessionCompletedIntegrationEvent for session {SessionId}", notification.FileUploadSession.Id);
                await _integrationEventPublisher.PublishAsync(publishMessage, cancellationToken);
            }
        }

    }
}
