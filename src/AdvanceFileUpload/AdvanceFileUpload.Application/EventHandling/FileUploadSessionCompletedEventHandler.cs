using AdvanceFileUpload.Application.Compression;
using AdvanceFileUpload.Application.FileProcessing;
using AdvanceFileUpload.Application.Settings;
using AdvanceFileUpload.Domain;
using AdvanceFileUpload.Domain.Core;
using AdvanceFileUpload.Domain.Events;
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

        public FileUploadSessionCompletedEventHandler(IRepository<FileUploadSession> fileUploadSessionRepository, IFileProcessor fileProcessor, IFileCompressor fileCompressor, IOptions<UploadSetting> uploadSetting, ILogger<FileUploadSessionCompletedEventHandler> logger)
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
            await _fileProcessor.ConcatenateChunksAsync(chunkPaths, outputFilePath, cancellationToken);

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

            _logger.LogInformation("FileUploadSessionCompletedEvent handled successfully for session {SessionId}", notification.FileUploadSession.Id);
        }

    }
}
