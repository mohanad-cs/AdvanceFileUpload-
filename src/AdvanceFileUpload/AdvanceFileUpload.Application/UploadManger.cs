using AdvanceFileUpload.Application.Compression;
using AdvanceFileUpload.Application.FileProcessing;
using AdvanceFileUpload.Application.Request;
using AdvanceFileUpload.Application.Response;
using AdvanceFileUpload.Application.Settings;
using AdvanceFileUpload.Application.Validators;
using AdvanceFileUpload.Domain;
using AdvanceFileUpload.Domain.Core;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdvanceFileUpload.Application
{
    /// <summary>
    /// Represents the upload manager.
    /// </summary>
    public class UploadManger : IUploadManger
    {
        private readonly IRepository<FileUploadSession> _repository;
        private readonly IDomainEventPublisher _domainEventPublisher;
        private readonly IFileValidator _fileValidator;
        private readonly IChunkValidator _chunkValidator;
        private readonly IFileProcessor _fileProcessor;
        private readonly IFileCompressor _fileCompressor;
        private readonly UploadSetting _uploadSetting;
        private readonly ILogger<UploadManger> _logger;


        ///<summary>
        /// Initializes a new instance of the <see cref="UploadManger"/> class.
        /// </summary>
        /// <param name="repository">The repository for managing file upload sessions.</param>
        /// <param name="domainEventPublisher">The publisher for domain events.</param>
        /// <param name="fileValidator">The validator for file properties.</param>
        /// <param name="chunkValidator">The validator for file chunks.</param>
        /// <param name="uploadSetting">The settings for file uploads.</param>
        /// <param name="fileProcessor">The service for file operations.</param>
        /// <param name="fileCompressor">The service for file compressor</param>
        /// <param name="logger">The logger for logging information.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when any of the parameters are null.
        /// </exception>
        public UploadManger(IRepository<FileUploadSession> repository, IDomainEventPublisher domainEventPublisher, IFileValidator fileValidator, IChunkValidator chunkValidator, IOptions<UploadSetting> uploadSetting, IFileProcessor fileProcessor , IFileCompressor fileCompressor, ILogger<UploadManger> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _domainEventPublisher = domainEventPublisher ?? throw new ArgumentNullException(nameof(domainEventPublisher));
            _fileValidator = fileValidator ?? throw new ArgumentNullException(nameof(fileValidator));
            _chunkValidator = chunkValidator ?? throw new ArgumentNullException(nameof(chunkValidator));
            _fileProcessor = fileProcessor ?? throw new ArgumentNullException(nameof(fileProcessor));
            _fileCompressor = fileCompressor ?? throw new ArgumentNullException(nameof(fileCompressor));
            _uploadSetting = uploadSetting.Value ?? throw new ArgumentNullException(nameof(uploadSetting));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<CreateUploadSessionResponse> CreateUploadSessionAsync(CreateUploadSessionRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Creating a new file upload session");
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(_uploadSetting.SavingDirectory))
            {
                _logger.LogWarning("The SavingDirectory have not been Configured ");
                throw new ApplicationException("The SavingDirectory have not been Configured ");
            }
            if (!_fileValidator.IsValidFileName(request.FileName))
            {
                throw new ApplicationException("The File Name is not valid");
            }
            if (!_fileValidator.IsValidFileExtension(request.FileExtension, _uploadSetting.AllowedExtensions))
            {
                throw new ApplicationException("The File Extension is not allowed");
            }
            if (!_fileValidator.IsValidFileSize(request.FileSize, _uploadSetting.MaxFileSize))
            {
                throw new ApplicationException("The File Size is Not int the allowed  rang of File Size");
            }
            if (request.UseCompression && !_uploadSetting.EnableCompression)
            {
                throw new ApplicationException("The Compression is not enabled on the server");
            }
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(_uploadSetting.SavingDirectory);
            var session = new FileUploadSession(request.FileName, _uploadSetting.SavingDirectory, request.FileSize, _uploadSetting.MaxChunkSize);
            await _repository.AddAsync(session, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            foreach (var domainEvent in session.DomainEvents)
            {
                await _domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
            }
            session.ClearDomainEvents();
            _logger.LogInformation($"The file upload session has been created successfully With Session Id [{session.Id}]");
            return new CreateUploadSessionResponse()
            {
                SessionId = session.Id,
                FileSize = session.FileSize,
                MaxChunkSize = session.MaxChunkSize,
                SessionStartDate = session.SessionStartDate,
                TotalChunksToUpload = session.TotalChunksToUpload,
                UploadStatus = (UploadStatus)session.Status,
            };
        }
        ///<inheritdoc/>
        public async Task<bool> CompleteUploadSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Completing the file upload session");
            if (sessionId == Guid.Empty)
            {
                throw new ApplicationException("the session Id is Not Valid");
            }
            var session = await _repository.GetByIdAsync(sessionId, cancellationToken);
            if (session is null)
            {
                throw new ApplicationException($"The session with the given Id {sessionId} is not found");
            }
            
            session.CompleteSession();
            await _repository.UpdateAsync(session);
            await _repository.SaveChangesAsync(cancellationToken);
            foreach (var domainEvent in session.DomainEvents)
            {
                await _domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
            }
            session.ClearDomainEvents();
            _logger.LogInformation($"The file upload session  With Session Id [{session.Id}], has been completed successfully");
            return true;

        }
        ///<inheritdoc/>
        public async Task<bool> UploadChunkAsync(UploadChunkRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Uploading a chunk of the file");
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.SessionId == Guid.Empty)
            {
                throw new ApplicationException("The Session Id is Not Valid");
            }
            if (!_chunkValidator.IsValidChunkIndex(request.ChunkIndex))
            {
                throw new ApplicationException($"The Chunk Index {request.ChunkIndex} is Not Valid");
            }
            if (!_chunkValidator.IsValidChunkSize(request.ChunkData.LongLength, _uploadSetting.MaxChunkSize))
            {
                throw new ApplicationException("The Chunk Size is greater than the Maximum Chunk Size");
            }
            cancellationToken.ThrowIfCancellationRequested();

            var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
            if (session is null)
            {
                throw new ApplicationException($"The session with the given Id {request.SessionId} is not found");
            }
            await _fileProcessor.SaveFileAsync($"{session.Id}_{request.ChunkIndex}.chunk", request.ChunkData, _uploadSetting.TempDirectory, cancellationToken);
            session.AddChunk(request.ChunkIndex, Path.Combine(_uploadSetting.TempDirectory, $"{session.Id}_{request.ChunkIndex}.chunk"));
            await _repository.UpdateAsync(session, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            foreach (var domainEvent in session.DomainEvents)
            {
                await _domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
            }
            session.ClearDomainEvents();
            _logger.LogInformation($"The chunk [{request.ChunkIndex}] of the file With Session Id [{session.Id}],  has been uploaded successfully");
            return true;
        }




        ///<inheritdoc/>
        public async Task<UploadSessionStatusResponse?> GetUploadSessionStatusAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            if (sessionId == Guid.Empty)
            {
                throw new ApplicationException("the session Id is Not Valid");
            }
            var session = await _repository.GetByIdAsync(sessionId, cancellationToken);

            if (session is null)
            {
                throw new ApplicationException($"The session with the given Id {sessionId} is not found");
            }
            else
            {
                return new UploadSessionStatusResponse()
                {
                    SessionId = session.Id,
                    FileSize = session.FileSize,
                    MaxChunkSize = session.MaxChunkSize,
                    SessionStartDate = session.SessionStartDate,
                    TotalChunksToUpload = session.TotalChunksToUpload,
                    UploadStatus = (UploadStatus)session.Status,
                    ProgressPercentage = session.ProgressPercentage,
                    RemainChunks = session.GetRemainChunks(),
                    SessionEndDate = session.SessionEndDate,
                    TotalUploadedChunks = session.TotalUploadedChunks,

                };

            }
        }
        ///<inheritdoc/>
        public async Task<bool> CancelUploadSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Canceling the file upload session id {sessionId}");
            if (sessionId == Guid.Empty)
            {
                throw new ApplicationException("the session Id is Not Valid");
            }
            var session = await _repository.GetByIdAsync(sessionId, cancellationToken);
            if (session is null)
            {
                throw new ApplicationException($"The session with the given Id {sessionId} is not found");
            }
            session.CancelSession();
            await _repository.UpdateAsync(session, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            foreach (var domainEvent in session.DomainEvents)
            {
                await _domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
            }
            session.ClearDomainEvents();
            _logger.LogInformation($"The file upload session  With Session Id [{session.Id}], has been canceled successfully");
            return true;
        }
        ///<inheritdoc/>
        public async Task<bool> PauseUploadSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Pausing the file upload session id {sessionId} ",sessionId);
            if (sessionId == Guid.Empty)
            {
                throw new ApplicationException("the session Id is Not Valid");
            }
            var session = await _repository.GetByIdAsync(sessionId, cancellationToken);
            if (session is null)
            {
                throw new ApplicationException($"The session with the given Id {sessionId} is not found");
            }
            session.PauseSession();
            await _repository.UpdateAsync(session, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            foreach (var domainEvent in session.DomainEvents)
            {
                await _domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
            }
            session.ClearDomainEvents();
            _logger.LogInformation("The file upload session  With Session Id [{sessionId}], has been paused successfully",session.Id);
            return true;

        }
    }
}