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
            if (notification.FileUploadSession.UseCompression)
            {
               await _fileCompressor.DecompressFilesAsync(notification.FileUploadSession.ChunkFiles.OrderBy(x => x.ChunkIndex).Select(x => x.ChunkPath).ToArray(),
                                                          _uploadSetting.TempDirectory,
                                                          (CompressionAlgorithmOption)notification.FileUploadSession.CompressionAlgorithm,
                                                          cancellationToken);
                List<string> decompressedchunkFiles = new List<string>();
                foreach (var chunk in notification.FileUploadSession.ChunkFiles)
                {
                    decompressedchunkFiles.Add(Path.Combine(_uploadSetting.TempDirectory, Path.GetFileName(chunk.ChunkPath.Replace(".gz",string.Empty))));
                }
               await _fileProcessor.ConcatenateChunksAsync(decompressedchunkFiles,
                                                           Path.Combine(notification.FileUploadSession.SavingDirectory, notification.FileUploadSession.FileName),
                                                           cancellationToken);
                foreach (var chunk in notification.FileUploadSession.ChunkFiles)
                {
                    File.Delete(chunk.ChunkPath);
                }
            }
            else
            {
               
                await _fileProcessor.ConcatenateChunksAsync(notification.FileUploadSession.ChunkFiles.Select(x=>x.ChunkPath).ToList(),
                                                            Path.Combine(notification.FileUploadSession.SavingDirectory, notification.FileUploadSession.FileName),
                                                            cancellationToken);
                foreach (var chunk in notification.FileUploadSession.ChunkFiles)
                {
                    File.Delete(chunk.ChunkPath);
                }
            }
        }
    }
}
