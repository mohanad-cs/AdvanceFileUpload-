using AdvanceFileUpload.Application.Compression;
using AdvanceFileUpload.Application.FileProcessing;
using AdvanceFileUpload.Application.Settings;
using AdvanceFileUpload.Domain.Events;
using AdvanceFileUpload.Integration.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdvanceFileUpload.Application.EventHandling
{
    /// <summary>
    /// Handles the <see cref="FileUploadSessionCompletedEvent"/> to process the uploaded file session.
    /// </summary>
    public sealed class FileUploadSessionCompletedEventHandler : INotificationHandler<FileUploadSessionCompletedEvent>
    {
        private readonly ILogger<FileUploadSessionCompletedEventHandler> _logger;
        private readonly IFileCompressor _fileCompressor;
        private readonly IFileProcessor _fileProcessor;
        private readonly UploadSetting _uploadSetting;
        private readonly IIntegrationEventPublisher _integrationEventPublisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadSessionCompletedEventHandler"/> class.
        /// </summary>
        /// <param name="fileProcessor">The file processor to handle file operations.</param>
        /// <param name="fileCompressor">The file compressor to handle compression and decompression.</param>
        /// <param name="uploadSetting">The upload settings configuration.</param>
        /// <param name="integrationEventPublisher">The integration event publisher for publishing events.</param>
        /// <param name="logger">The logger instance for logging operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the required dependencies are null.</exception>
        public FileUploadSessionCompletedEventHandler(
            IFileProcessor fileProcessor,
            IFileCompressor fileCompressor,
            IOptions<UploadSetting> uploadSetting,
            IIntegrationEventPublisher integrationEventPublisher,
            ILogger<FileUploadSessionCompletedEventHandler> logger)
        {
            if (uploadSetting is null)
            {
                throw new ArgumentNullException(nameof(uploadSetting));
            }
            _fileProcessor = fileProcessor ?? throw new ArgumentNullException(nameof(fileProcessor));
            _fileCompressor = fileCompressor ?? throw new ArgumentNullException(nameof(fileCompressor));
            _uploadSetting = uploadSetting.Value;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _integrationEventPublisher = integrationEventPublisher;
        }

        /// <summary>
        /// Handles the <see cref="FileUploadSessionCompletedEvent"/> to process the uploaded file session.
        /// </summary>
        /// <param name="notification">The event notification containing the file upload session details.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the notification is null.</exception>
        public async Task Handle(FileUploadSessionCompletedEvent notification, CancellationToken cancellationToken)
        {
            if (notification is null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            _logger.LogInformation("Handling FileUploadSessionCompletedEvent for session {SessionId}", notification.FileUploadSession.Id);

            // Concatenate file chunks
            var chunkPaths = notification.FileUploadSession.ChunkFiles.OrderBy(x => x.ChunkIndex).Select(x => x.ChunkPath).ToList();
            string fileNamePostFix = notification.FileUploadSession.UseCompression ? ".gz" : string.Empty;
            var outputFilePath = Path.Combine(notification.FileUploadSession.SavingDirectory, notification.FileUploadSession.FileName + fileNamePostFix);

            _logger.LogInformation("Concatenating chunks for session {SessionId}", notification.FileUploadSession.Id);
            await _fileProcessor.MergeChunksAsync(chunkPaths, outputFilePath, cancellationToken);

            // Delete chunk files
            foreach (var chunk in notification.FileUploadSession.ChunkFiles)
            {
                _logger.LogInformation("Deleting chunk file {ChunkPath} for session {SessionId}", chunk.ChunkPath, notification.FileUploadSession.Id);
                File.Delete(chunk.ChunkPath);
            }

            // Decompress file if required
            if (notification.FileUploadSession.UseCompression)
            {
                _logger.LogInformation("Decompressing file for session {SessionId}", notification.FileUploadSession.Id);
                var tempDecompressedFilePath = Path.Combine(_uploadSetting.TempDirectory, Path.GetFileNameWithoutExtension(outputFilePath));
                await _fileCompressor.DecompressFileAsync(outputFilePath, _uploadSetting.TempDirectory, (CompressionAlgorithmOption)notification.FileUploadSession.CompressionAlgorithm, cancellationToken);
                File.Move(tempDecompressedFilePath, outputFilePath.Replace(".gz", string.Empty), true);
                File.Delete(outputFilePath);
            }

            _logger.LogInformation("File Upload successfully Completed for session {SessionId}", notification.FileUploadSession.Id);

            // Publish integration event if enabled
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
                    SessionEndDateTime = notification.FileUploadSession.SessionEndDate,
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
