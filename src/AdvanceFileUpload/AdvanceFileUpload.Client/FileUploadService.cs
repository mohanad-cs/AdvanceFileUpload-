using AdvanceFileUpload.Application.Compression;
using AdvanceFileUpload.Application.FileProcessing;
using AdvanceFileUpload.Application.Request;
using AdvanceFileUpload.Application.Response;
using AdvanceFileUpload.Application.Shared;
using AdvanceFileUpload.Client.Helper;
using Microsoft.AspNetCore.SignalR.Client;
using Polly.Retry;
using Polly;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace AdvanceFileUpload.Client
{
    /// <summary>
    /// Provides functionality to upload files with support for compression, chunking, and session management.
    /// </summary>
    /// <remarks>Note that the <see cref="FileUploadService"/> have been build to process a single upload request.<br></br>
    /// if you want to upload multiple files at the same time, you need to create a new instance of the <see cref="FileUploadService"/> for each file.</remarks>
    public class FileUploadService : IFileUploadService, IDisposable
    {
        #region Fields
        private readonly HttpClient _httpClient;
        private readonly IFileProcessor _fileProcessor;
        private readonly IFileCompressor _fileCompressor;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly UploadOptions _uploadOptions;
        private List<string> _chunksToUpload = new();
        private Guid _sessionId;
        private long _chunkSize;
        private int _totalChunksToUpload;
        private string? _originalFilePath;
        private SessionStatus _sessionStatus = SessionStatus.None;
        private readonly SemaphoreSlim _semaphore;
        private readonly HubConnection _hubConnection;
        private long _originalFileSize => _originalFilePath is null ? 0 : new FileInfo(_originalFilePath).Length;
        private string? _compressedFilePath;
        private bool _disposed;
        private string? _hubConnectionsId;
        private long? _compressedFileSize => _compressedFilePath is null ? null : new FileInfo(_compressedFilePath).Length;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private readonly ILogger<FileUploadService> _logger;

        #endregion

        #region Properties
        /// <inheritdoc />
        public bool CanPauseSession => _sessionStatus == SessionStatus.Uploading;
        /// <inheritdoc />
        public bool CanCancelSession => _sessionStatus != SessionStatus.Completed && _sessionStatus != SessionStatus.Canceled;
        /// <inheritdoc />
        public bool CanResumeSession => _sessionStatus == SessionStatus.Paused;
        /// <inheritdoc />
        public bool IsSessionPaused => _sessionStatus == SessionStatus.Paused;
        /// <inheritdoc />
        public bool IsSessionCanceled => _sessionStatus == SessionStatus.Canceled;
        /// <inheritdoc />
        public bool IsSessionCompleted => _sessionStatus == SessionStatus.Completed;
        #endregion

        #region Events
        /// <summary>
        /// Occurs when a new upload session is created.
        /// </summary>
        public event EventHandler<SessionCreatedEventArgs>? SessionCreated;
        /// <summary>
        /// Occurs when a chunk is uploaded.
        /// </summary>
        public event EventHandler<ChunkUploadedEventArgs>? ChunkUploaded;
        /// <summary>
        /// Occurs when the upload session is completed.
        /// </summary>
        public event EventHandler<SessionCompletedEventArgs>? SessionCompleted;
        /// <summary>
        /// Occurs when the upload session is canceled.
        /// </summary>
        public event EventHandler<SessionCanceledEventArgs>? SessionCanceled;
        /// <summary>
        /// Occurs when the upload session is paused.
        /// </summary>
        public event EventHandler<SessionPausedEventArgs>? SessionPaused;
        /// <summary>
        /// Occurs when the upload session is resumed.
        /// </summary>
        public event EventHandler<SessionResumedEventArgs>? SessionResumed;
        /// <summary>
        /// Occurs when the upload progress changes.
        /// </summary>
        public event EventHandler<UploadProgressChangedEventArgs>? UploadProgressChanged;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadService"/> class.
        /// </summary>
        /// <param name="apiBaseAddress">The base address of the API.</param>
        /// <param name="uploadOptions">The upload options.</param>
        public FileUploadService(Uri apiBaseAddress, UploadOptions uploadOptions)
        {
            _logger = LoggerFactoryHelper.CreateLogger<FileUploadService>();
            _httpClient = new HttpClient()
            {
                BaseAddress = apiBaseAddress ?? throw new ArgumentNullException(nameof(apiBaseAddress)),
                Timeout = TimeSpan.FromMinutes(10)


            };
            _uploadOptions = uploadOptions ?? throw new ArgumentNullException(nameof(uploadOptions));
            _semaphore = new SemaphoreSlim(_uploadOptions.MaxConcurrentUploads, _uploadOptions.MaxConcurrentUploads);
            _fileProcessor = new FileProcessor();
            _fileCompressor = new FileCompressor(LoggerFactoryHelper.CreateLogger<FileCompressor>());
            _cancellationTokenSource = new CancellationTokenSource();
            var hubUri = new Uri($"{apiBaseAddress.AbsoluteUri}{RouteTemplates.UploadProcessHub}");
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUri)
                .WithAutomaticReconnect()
                .Build();
            _hubConnection.On<UploadSessionStatusNotification>("ReceiveUploadProcessNotification", status =>
            {
                OnUploadProgressChanged(UploadProgressChangedEventArgs.Create(status));
            });
            _hubConnection.Reconnected += _hubConnection_Reconnected;
            _hubConnection.Closed += _hubConnection_Closed;

            _retryPolicy = Policy.Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(
                    _uploadOptions.MaxRetriesCount,
                    retryAttempt => TimeSpan.FromMilliseconds(2000 * retryAttempt),
                    (outcome, timespan, retryAttempt, context) =>
                    {
                       
                    });
        }
        private async Task _hubConnection_Closed(Exception? arg)
        {
            _logger.LogWarning("Hub connection closed. Exception: {Exception}", arg);
            _hubConnectionsId = null;
            await Task.CompletedTask.ConfigureAwait(false);
        }

        /// <inheritdoc />
        /// <exception cref="ApplicationException"></exception>
        /// <remarks>Throws an <see cref="ApplicationException"/> if the service is already processing an upload request.</remarks>
        public async Task UploadFileAsync(string filePath)
        {
            _logger.LogInformation("Starting file upload for {FilePath}", filePath);
            if (_sessionStatus != SessionStatus.None)
            {
                _logger.LogWarning("Upload request already in progress.");
                throw new ApplicationException("The service is already processing an upload request.");
            }
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _logger.LogWarning("File path is null or whitespace.");
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or whitespace.", nameof(filePath));
            }
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File does not exist: {FilePath}", filePath);
                throw new ApplicationException("The file does not exist");
            }

            _cancellationTokenSource = new CancellationTokenSource();
            await _hubConnection.StartAsync().ConfigureAwait(false);
            _hubConnectionsId = _hubConnection.ConnectionId;
            _originalFilePath = filePath;

            if (_uploadOptions.CompressionOption != null)
            {
                _logger.LogInformation("Compressing file {FilePath}", filePath);
                _compressedFilePath = Path.Combine(_uploadOptions.TempDirectory, $"{Path.GetFileName(filePath)}.gz");
                await _fileCompressor.CompressFileAsync(filePath, _uploadOptions.TempDirectory, _uploadOptions.CompressionOption.Algorithm, _uploadOptions.CompressionOption.Level, _cancellationTokenSource.Token).ConfigureAwait(false);
            }

            var createSessionRequest = new CreateUploadSessionRequest
            {
                FileName = Path.GetFileName(_originalFilePath),
                FileSize = _originalFileSize,
                CompressedFileSize = _compressedFileSize,
                FileExtension = Path.GetExtension(_originalFilePath),
                Compression = _uploadOptions.CompressionOption,
                HubConnectionId = _hubConnectionsId,
            };

            var createSessionResponse = await _httpClient.PostAsJsonAsync(RouteTemplates.CreateSession, createSessionRequest, _cancellationTokenSource.Token).ConfigureAwait(false);
            createSessionResponse.EnsureSuccessStatusCode();
            var sessionResponse = await createSessionResponse.Content.ReadFromJsonAsync<CreateUploadSessionResponse>().ConfigureAwait(false);

            if (sessionResponse == null)
            {
                _logger.LogError("Failed to create upload session.");
                throw new ApplicationException("Failed to create upload session");
            }

            _sessionId = sessionResponse.SessionId;
            _chunkSize = sessionResponse.MaxChunkSize;
            _totalChunksToUpload = sessionResponse.TotalChunksToUpload;
            _originalFilePath = filePath;

            OnSessionCreated(SessionCreatedEventArgs.Create(sessionResponse));

            string? fileToSplit= _uploadOptions.CompressionOption != null ? _compressedFilePath : _originalFilePath;
            _chunksToUpload = await _fileProcessor.SplitFileIntoChunksAsync(fileToSplit!, _chunkSize, _uploadOptions.TempDirectory, _cancellationTokenSource.Token).ConfigureAwait(false);
            if (_chunksToUpload.Count != _totalChunksToUpload)
            {
                _logger.LogError("Error in splitting the file. Expected chunks: {ExpectedChunks}, Actual chunks: {ActualChunks}", _totalChunksToUpload, _chunksToUpload.Count);
                throw new ApplicationException($"Error in splitting the file. Expected chunks: {_totalChunksToUpload}, Actual chunks: {_chunksToUpload.Count}");
            }

            _logger.LogInformation("Uploading chunks in parallel.");
            var uploadTasks = _chunksToUpload.Select((chunkPath, index) => UploadChunkWithLimitAsync(chunkPath, index, _cancellationTokenSource.Token)).ToArray();
            await Task.WhenAll(uploadTasks).ConfigureAwait(false);

            await CompleteUpload().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task PauseUploadAsync()
        {
            if (CanPauseSession)
            {
                _logger.LogInformation("Pausing upload session {SessionId}", _sessionId);
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
                await _httpClient.PostAsJsonAsync($"{RouteTemplates.PauseSession}", new PauseUploadSessionRequest() { SessionId = _sessionId }, _cancellationTokenSource.Token).ConfigureAwait(false);
                OnSessionPaused(new SessionPausedEventArgs(_sessionId, Path.GetFileName(_originalFilePath), _originalFileSize));
            }
            else
            {
                _logger.LogWarning("Cannot pause session {SessionId}", _sessionId);
                throw new ApplicationException("The session cannot be paused");
            }
        }

        /// <inheritdoc />
        public async Task ResumeUploadAsync()
        {
            if (CanResumeSession)
            {
                _logger.LogInformation("Resuming upload session {SessionId}", _sessionId);
                _cancellationTokenSource = new CancellationTokenSource();
                OnSessionResumed(new SessionResumedEventArgs(_sessionId, Path.GetFileName(_originalFilePath), _originalFileSize));

                var statusResponse = await _httpClient.GetFromJsonAsync<UploadSessionStatusResponse>($"{RouteTemplates.SessionStatus}?sessionId={_sessionId}", _cancellationTokenSource.Token).ConfigureAwait(false);
                if (statusResponse != null)
                {
                    if (statusResponse.RemainChunks != null && statusResponse.RemainChunks.Any())
                    {
                        _logger.LogInformation("Uploading remaining chunks for session {SessionId}", _sessionId);
                        var remainingChunkPaths = statusResponse.RemainChunks.Select(index => _chunksToUpload[index]).ToList();
                        var uploadTasks = remainingChunkPaths.Select((chunkPath, index) => UploadChunkWithLimitAsync(chunkPath, statusResponse.RemainChunks[index], _cancellationTokenSource.Token)).ToArray();
                        await Task.WhenAll(uploadTasks).ConfigureAwait(false);

                        await CompleteUpload().ConfigureAwait(false);
                    }
                    else if (statusResponse.UploadStatus == UploadStatus.PendingToComplete)
                    {
                        await CompleteUpload().ConfigureAwait(false);
                    }
                    else if (statusResponse.UploadStatus == UploadStatus.Completed)
                    {
                        _sessionStatus = SessionStatus.Completed;
                    }
                }
                else
                {
                    _logger.LogError("Failed to get upload session status for session {SessionId}", _sessionId);
                    throw new ApplicationException("Failed to get upload session status");
                }
            }
        }

        /// <inheritdoc />
        public async Task CancelUploadAsync()
        {
            if (CanCancelSession)
            {
                _logger.LogInformation("Canceling upload session {SessionId}", _sessionId);
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
                var completeResponse = await ExecuteWithRetryPolicy(() => _httpClient.PostAsJsonAsync($"{RouteTemplates.CancelSession}", new CancelUploadSessionRequest() { SessionId = _sessionId }, _cancellationTokenSource.Token));
                completeResponse.EnsureSuccessStatusCode();
                OnSessionCanceled(new SessionCanceledEventArgs(_sessionId, Path.GetFileName(_originalFilePath), new FileInfo(_originalFilePath).Length));
            }
        }

        /// <summary>
        /// Uploads a chunk of the file asynchronously.
        /// </summary>
        /// <param name="chunkPath">The path of the chunk to be uploaded.</param>
        /// <param name="chunkIndex">The index of the chunk.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        private async Task UploadChunkAsync(string chunkPath, int chunkIndex, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Uploading chunk {ChunkIndex} for session {SessionId}", chunkIndex, _sessionId);
            var chunkData = await File.ReadAllBytesAsync(chunkPath, cancellationToken).ConfigureAwait(false);
            var uploadChunkRequest = new UploadChunkRequest
            {
                SessionId = _sessionId,
                ChunkIndex = chunkIndex,
                ChunkData = chunkData,
                HubConnectionId = _hubConnectionsId
            };
            //var response = await ExecuteWithRetryPolicy(() => _httpClient.PostAsJsonAsync(RouteTemplates.UploadChunk, uploadChunkRequest, cancellationToken));
            var response = await  _httpClient.PostAsJsonAsync(RouteTemplates.UploadChunk, uploadChunkRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            OnChunkUploaded(new ChunkUploadedEventArgs(_sessionId, chunkIndex, chunkData.Length));
        }

        /// <summary>
        /// Uploads a chunk of the file with concurrency limit asynchronously.
        /// </summary>
        /// <param name="chunkPath">The path of the chunk to be uploaded.</param>
        /// <param name="chunkIndex">The index of the chunk.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        private async Task UploadChunkWithLimitAsync(string chunkPath, int chunkIndex, CancellationToken cancellationToken)
        {
            // await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            // try
            //  {
                Thread.Sleep(1000);
                await UploadChunkAsync(chunkPath, chunkIndex, cancellationToken).ConfigureAwait(false);
           // }
            //finally
            //{
            //    _semaphore.Release();
            //}
        }

        /// <summary>
        /// Completes the upload session asynchronously.
        /// </summary>
        private async Task CompleteUpload()
        {
            if (_sessionStatus != SessionStatus.Completed && _sessionStatus != SessionStatus.Canceled)
            {
                _logger.LogInformation("Completing upload session {SessionId}", _sessionId);
                var completeResponse = await ExecuteWithRetryPolicy(() => _httpClient.PostAsJsonAsync($"{RouteTemplates.CompleteSession}", new CompleteUploadSessionRequest() { SessionId = _sessionId }, _cancellationTokenSource.Token));
                completeResponse.EnsureSuccessStatusCode();
                OnSessionCompleted(new SessionCompletedEventArgs(_sessionId, Path.GetFileName(_originalFilePath), new FileInfo(_originalFilePath).Length));
            }
            else
            {
                _logger.LogWarning("Cannot complete session {SessionId}", _sessionId);
                throw new ApplicationException("The session cannot be completed");
            }
        }

      
        /// <summary>
        /// Executes an HTTP request with a retry policy.
        /// </summary>
        /// <param name="action">The HTTP request action to execute.</param>
        /// <returns>The HTTP response message.</returns>
        private async Task<HttpResponseMessage> ExecuteWithRetryPolicy(Func<Task<HttpResponseMessage>> action)
        {
            return await _retryPolicy.ExecuteAsync(action).ConfigureAwait(false);
        }

        /// <summary>
        /// Handles the event when the hub connection is reconnected.
        /// </summary>
        /// <param name="arg">The new connection ID.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task _hubConnection_Reconnected(string? arg)
        {
            _logger.LogInformation("Hub connection reconnected. New connection ID: {ConnectionId}", arg);
            _hubConnectionsId = arg;
            await Task.CompletedTask.ConfigureAwait(false);
        }
        #endregion

        #region Event Raisers
        /// <summary>
        /// Raises the <see cref="SessionCreated"/> event.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected virtual void OnSessionCreated(SessionCreatedEventArgs e)
        {
            _sessionStatus = SessionStatus.Created;
            SessionCreated?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="ChunkUploaded"/> event.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected virtual void OnChunkUploaded(ChunkUploadedEventArgs e)
        {
            _sessionStatus = SessionStatus.Uploading;
            ChunkUploaded?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="SessionCompleted"/> event.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected virtual void OnSessionCompleted(SessionCompletedEventArgs e)
        {
            _sessionStatus = SessionStatus.Completed;
            SessionCompleted?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="SessionCanceled"/> event.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected virtual void OnSessionCanceled(SessionCanceledEventArgs e)
        {
            _sessionStatus = SessionStatus.Canceled;
            SessionCanceled?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="SessionPaused"/> event.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected virtual void OnSessionPaused(SessionPausedEventArgs e)
        {
            _sessionStatus = SessionStatus.Paused;
            SessionPaused?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="SessionResumed"/> event.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected virtual void OnSessionResumed(SessionResumedEventArgs e)
        {
            SessionResumed?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="UploadProgressChanged"/> event.
        /// </summary>
        /// <param name="e">The event data.</param>
        protected virtual void OnUploadProgressChanged(UploadProgressChangedEventArgs e)
        {
            UploadProgressChanged?.Invoke(this, e);
        }
        #endregion

        #region IDisposable Implementation
        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="FileUploadService"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient.Dispose();
                    _semaphore.Dispose();
                    _hubConnection.DisposeAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                    DeleteTempFiles();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Deletes temporary files created during the upload process.
        /// </summary>
        private void DeleteTempFiles()
        {
            if (_compressedFilePath != null)
            {
                File.Delete(_compressedFilePath);
            }
            if (_chunksToUpload != null)
            {
                foreach (var chunkPath in _chunksToUpload)
                {
                    File.Delete(chunkPath);
                }
            }
        }
        #endregion
    }
}


