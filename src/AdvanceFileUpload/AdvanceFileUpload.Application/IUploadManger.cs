using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdvanceFileUpload.Application.Shared;
using AdvanceFileUpload.Domain;
using AdvanceFileUpload.Domain.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AdvanceFileUpload.Application.Exception;

namespace AdvanceFileUpload.Application
{
    /// <summary>
    /// Interface for managing file upload sessions.
    /// </summary>
    public interface IUploadManger
    {
        /// <summary>
        /// Creates a new file upload session asynchronously.
        /// </summary>
        /// <param name="request">The request containing the details of the file to be uploaded.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response with the details of the created upload session.</returns>
        Task<CreateUploadSessionResponse> CreateUploadSessionAsync(CreateUploadSessionRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Completes the file upload session asynchronously.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the upload session.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response with the status of the completed upload session.</returns>
        Task<UploadSessionStatusResponse> CompleteUploadSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a chunk of the file asynchronously.
        /// </summary>
        /// <param name="request">The request containing the details of the chunk to be uploaded.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the chunk was uploaded successfully.</returns>
        Task<bool> UploadChunkAsync(UploadChunkRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the status of the upload session asynchronously.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the upload session.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response with the status of the upload session.</returns>
        Task<UploadSessionStatusResponse?> GetUploadSessionStatusAsync(Guid sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels the file upload session asynchronously.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the upload session.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the session was canceled successfully.</returns>
        Task<bool> CancelUploadSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    }


    public class UploadManger : IUploadManger
    {
        private readonly IRepository<FileUploadSession> _repository;
        private readonly IDomainEventPublisher _domainEventPublisher;
        private readonly IFileValidator _fileValidator;
        private readonly IChunkValidator _chunkValidator;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UploadManger> _logger;

        public UploadManger(IRepository<FileUploadSession> repository, IDomainEventPublisher domainEventPublisher, IFileValidator fileValidator, IChunkValidator chunkValidator, IConfiguration configuration, ILogger<UploadManger> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _domainEventPublisher = domainEventPublisher ?? throw new ArgumentNullException(nameof(domainEventPublisher));
            _fileValidator = fileValidator ?? throw new ArgumentNullException(nameof(fileValidator));
            _chunkValidator = chunkValidator ?? throw new ArgumentNullException(nameof(chunkValidator));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<CreateUploadSessionResponse> CreateUploadSessionAsync(CreateUploadSessionRequest request, CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            string? savingDirectory = _configuration["SavingDirectory"];
            long? maxChunkSize = long.Parse(_configuration["MaxChunkSize"]);
            if (string.IsNullOrWhiteSpace(savingDirectory))
            {
                throw new Exception.ApplicationException("The SavingDirectory have not been Configured ");
            }
            if (!maxChunkSize.HasValue || maxChunkSize.Value <= 0)
            {
                throw new Exception.ApplicationException("The MaxChunkSize have not been Configured");
            }
            Directory.CreateDirectory(savingDirectory);

            var session = new FileUploadSession(request.FileName, savingDirectory, request.FileSize, maxChunkSize.Value);
            await _repository.AddAsync(session, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            foreach (var domainEvent in session.DomainEvents)
            {
                await _domainEventPublisher.PublishAsync(domainEvent, cancellationToken);
            }
            return new CreateUploadSessionResponse()
            {
                SessionId = session.Id,
                FileSize = session.FileSize,
                MaxMaxChunkSize = session.MaxChunkSize,
                SessionStartDate = session.SessionStartDate,
                TotalChunksToUpload = session.TotalChunksToUpload,
                UploadStatus = (UploadStatus)session.Status,
            };
        }
        ///<inheritdoc/>
        public Task<UploadSessionStatusResponse> CompleteUploadSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public Task<bool> UploadChunkAsync(UploadChunkRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        public async Task<UploadSessionStatusResponse?> GetUploadSessionStatusAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            if (sessionId == Guid.Empty)
            {
                throw new Exception.ApplicationException("the session Id is Not Valid");
            }
            var session = await _repository.GetByIdAsync(sessionId, cancellationToken);

            if (session is null)
            {
                return null;
            }
            else
            {
                return new UploadSessionStatusResponse()
                {
                    SessionId = session.Id,
                    FileSize = session.FileSize,
                    MaxMaxChunkSize = session.MaxChunkSize,
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
        public Task<bool> CancelUploadSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
