using System.Net;
using System.Net.Http.Json;
using AdvanceFileUpload.Application.Compression;
using AdvanceFileUpload.Application.FileProcessing;
using AdvanceFileUpload.Application.Request;
using AdvanceFileUpload.Application.Response;
using AdvanceFileUpload.Application.Shared;
using AdvanceFileUpload.Client.Helper;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.Retry;
namespace AdvanceFileUpload.Client
{
    /// <summary>
    /// Provides functionality to upload files with support for compression, chunking, session management, and real-time progress tracking.
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="FileUploadService"/> handles complete file upload lifecycle including:</para>
    /// <list type="bullet">
    ///     <item>File compression using configurable algorithms</item>
    ///     <item>Chunked uploads with configurable chunk sizes</item>
    ///     <item>Automatic retries with exponential backoff</item>
    ///     <item>Network health monitoring</item>
    ///     <item>Real-time progress updates via SignalR</item>
    ///     <item>Pause/resume capabilities</item>
    /// </list>
    /// <para><strong>Event Usage Pattern:</strong></para>
    /// <list type="bullet">
    ///     <item>Subscribe to lifecycle events (<see cref="SessionCreated"/>, <see cref="SessionCompleted"/>) for session tracking</item>
    ///     <item>Use <see cref="UploadProgressChanged"/> for real-time progress updates</item>
    ///     <item>Handle <see cref="ChunkUploaded"/> for per-chunk tracking</item>
    ///     <item>Listen to error events (<see cref="UploadError"/>, <see cref="NetworkError"/>) for error handling</item>
    ///     <item>Use pause/resume events (<see cref="SessionPaused"/>, <see cref="SessionResumed"/>) for UI state management</item>
    /// </list>
    /// <para><strong>Typical Event Sequence:</strong></para>
    /// <list type="number">
    ///     <item><see cref="FileCompressionStarted"/></item>
    ///     <item><see cref="FileCompressionCompleted"/></item>
    ///     <item><see cref="SessionCreated"/></item>
    ///     <item><see cref="FileSplittingStarted"/></item>
    ///     <item><see cref="FileSplittingCompleted"/></item>
    ///     <item>Multiple <see cref="ChunkUploaded"/> events</item>
    ///     <item><see cref="SessionCompleted"/></item>
    /// </list>
    /// <para><strong>Important Notes:</strong></para>
    /// <list type="bullet">
    ///     <item>Subscribe to events before calling <see cref="UploadFileAsync"/> to ensure proper event capture</item>
    ///     <item>Use <see cref="Dispose()"/> when upload session is completed to clean up temporary files</item>
    ///     <item>Service instances are single-use - if you want to upload multiple files in parallel, you need to create a new instance of the <see cref="FileUploadService"/> for each file.</item>
    ///     <item>Network connectivity is automatically monitored with <see cref="NetworkError"/> reporting</item>
    ///     <item>The service will Ignore compression for already compressed file extensions. You can get the already compressed file extensions by using the <see cref="CompressionIgnoredExtensions"/><br></br>
    ///     you can add your own extensions to the list of ignored extensions by using the <see cref="UploadOptions.ExcludedCompressionExtensions"/> property in the <see cref="UploadOptions"/> class.</item>
    /// </list>
    /// </remarks>
    public sealed class FileUploadService : IFileUploadService
    {
        #region Fields
        /// <summary>
        /// The HTTP client used for making API requests.
        /// </summary>
        private readonly HttpClient _httpClient;
        /// <summary>
        /// The file processor used for splitting and merging files.
        /// </summary>
        private readonly IFileProcessor _fileProcessor;
        /// <summary>
        /// The file compressor used for compressing and decompressing files.
        /// </summary>
        private readonly IFileCompressor _fileCompressor;
        /// <summary>
        /// The cancellation token source used for canceling asynchronous operations.
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;
        /// <summary>
        /// The options for uploading files.
        /// </summary>
        private readonly UploadOptions _uploadOptions;
        /// <summary>
        /// The list of chunks to be uploaded.
        /// </summary>
        private List<string> _chunksToUpload = new();
        /// <summary>
        /// The unique identifier for the upload session.
        /// </summary>
        private Guid _sessionId;
        /// <summary>
        /// The size of each chunk in bytes.
        /// </summary>
        private long _chunkSize;
        /// <summary>
        /// The total number of chunks to be uploaded.
        /// </summary>
        private int _totalChunksToUpload;
        /// <summary>
        /// The original file path of the file to be uploaded.
        /// </summary>
        private string? _originalFilePath;
        /// <summary>
        /// The semaphore used to limit the number of concurrent uploads.
        /// </summary>
        private readonly SemaphoreSlim _semaphore;
        /// <summary>
        /// The SignalR hub connection used for real-time notifications.
        /// </summary>
        private readonly HubConnection _hubConnection;
        /// <summary>
        /// The size of the original file in bytes.
        /// </summary>
        private long _originalFileSize => _originalFilePath is null ? 0 : new FileInfo(_originalFilePath).Length;
        /// <summary>
        /// The path of the compressed file.
        /// </summary>
        private string? _compressedFilePath;
        /// <summary>
        /// Indicates whether the object has been disposed.
        /// </summary>
        private bool _disposed;
        /// <summary>
        /// The connection ID of the SignalR hub.
        /// </summary>
        private string? _hubConnectionsId;
        /// <summary>
        /// The size of the compressed file in bytes.
        /// </summary>
        private long? _compressedFileSize => _compressedFilePath is null ? null : new FileInfo(_compressedFilePath).Length;
        /// <summary>
        /// The retry policy for HTTP requests.
        /// </summary>
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        /// <summary>
        /// The logger used for logging information and errors.
        /// </summary>
        private readonly ILogger<FileUploadService> _logger;
        /// <summary>
        /// The lock object used for synchronizing access to the session status.
        /// </summary>
        private readonly object _statusLock = new();
        /// <summary>
        /// The current status of the upload session.Don't use it directly, use the <see cref="_sessionStatus"/> property instead.
        /// </summary>
        private SessionStatus sessionStatus;
        /// <summary>
        /// The current status of the upload session with thread-safe access.
        /// </summary>
        private SessionStatus _sessionStatus
        {
            get { lock (_statusLock) return sessionStatus; }
            set { lock (_statusLock) sessionStatus = value; }
        }
        /// <summary>
        /// The cached connection status of the network.
        /// </summary>
        private ConnectionStatus _cachedConnectionStatus;
        /// <summary>
        /// The network connection checker used for checking the health of the API.
        /// </summary>
        private readonly INetworkConnectionChecker _networkConnectionChecker;
        /// <summary>
        /// The time of the last health check.
        /// </summary>
        private TimeSpan _lastHealthCheck;
        /// <summary>
        /// Indicates whether the file has been compressed.
        /// </summary>
        private bool _fileHasBeenCompressed;
        /// <summary>
        /// Indicates whether the file has been split into chunks.
        /// </summary>
        private bool _fileHasBeenSplit;
        #endregion Fields

        #region Properties
        /// <inheritdoc />
        public bool CanPauseSession => _sessionStatus == SessionStatus.Uploading || _sessionStatus == SessionStatus.None;
        /// <inheritdoc />
        public bool CanCancelSession => _sessionStatus != SessionStatus.Completed && _sessionStatus != SessionStatus.Canceled && _sessionId != Guid.Empty;
        /// <inheritdoc />
        public bool CanResumeSession => _sessionStatus == SessionStatus.Paused || _sessionStatus == SessionStatus.None;
        /// <inheritdoc />
        public bool IsSessionPaused => _sessionStatus == SessionStatus.Paused;
        /// <inheritdoc />
        public bool IsSessionCanceled => _sessionStatus == SessionStatus.Canceled;
        /// <inheritdoc />
        public bool IsSessionCompleted => _sessionStatus == SessionStatus.Completed;
        ///<inheritdoc/>
        public IReadOnlyList<string> CompressionIgnoredExtensions => _fileCompressor.ExcludedExtension;
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
        /// <summary>
        /// Occurs when an upload error happens.
        /// </summary>
        public event EventHandler<string>? UploadError;
        /// <summary>
        /// Occurs when a network error happens.
        /// </summary>
        public event EventHandler<string>? NetworkError;
        /// <summary>
        /// Occurs when the file splitting process starts.
        /// </summary>
        public event EventHandler? FileSplittingStarted;
        /// <summary>
        /// Occurs when the file splitting process completes.
        /// </summary>
        public event EventHandler? FileSplittingCompleted;
        /// <summary>
        /// Occurs when the file compression process starts.
        /// </summary>
        public event EventHandler? FileCompressionStarted;
        /// <summary>
        /// Occurs when the file compression process completes.
        /// </summary>
        public event EventHandler? FileCompressionCompleted;
        /// <summary>
        /// Occurs when the upload session is pausing.
        /// </summary>
        public event EventHandler? SessionPausing;
        /// <summary>
        /// Occurs when the upload session is resuming.
        /// </summary>
        public event EventHandler? SessionResuming;
        /// <summary>
        /// Occurs when the upload session is canceling.
        /// </summary>
        public event EventHandler? SessionCanceling;
        /// <summary>
        /// Occurs when the upload session is completing.
        /// </summary>
        public event EventHandler? SessionCompleting;
        /// <summary>
        /// Occurs when an authentication error happens.
        /// </summary>
        public event EventHandler<string>? AuthenticationError;

        #endregion Events

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadService"/> class.
        /// </summary>
        /// <param name="apiBaseAddress">The base address of the API.</param>
        /// <param name="uploadOptions">The options for uploading files.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="apiBaseAddress"/> or <paramref name="uploadOptions"/> is null.
        /// </exception>
        public FileUploadService(Uri apiBaseAddress, UploadOptions uploadOptions)
        {
            try
            {
                _uploadOptions = uploadOptions ?? throw new ArgumentNullException(nameof(uploadOptions));
                _sessionStatus = SessionStatus.None;
                _logger = NullLogger<FileUploadService>.Instance;
                _httpClient = new HttpClient()
                {
                    BaseAddress = apiBaseAddress ?? throw new ArgumentNullException(nameof(apiBaseAddress)),
                    Timeout = _uploadOptions.RequestTimeOut,

                };
                _httpClient.DefaultRequestHeaders.Add("X-APIKEY", uploadOptions.APIKey);
                _semaphore = new SemaphoreSlim(_uploadOptions.MaxConcurrentUploads, _uploadOptions.MaxConcurrentUploads);
                _fileProcessor = new FileProcessor(NullLogger<FileProcessor>.Instance);
                _fileCompressor = new FileCompressor(NullLogger<FileCompressor>.Instance);
                _cancellationTokenSource = new CancellationTokenSource();
                _networkConnectionChecker = new NetworkConnectionChecker(new NetworkCheckOptions
                {
                    BaseAddress = apiBaseAddress,
                    HealthEndpoint = RouteTemplates.APIHealthEndPoint,
                    Method = HttpMethod.Get,
                    Timeout = TimeSpan.FromSeconds(5),
                    UseHttp2 = false
                });
                var hubUri = new Uri($"{apiBaseAddress.AbsoluteUri}{RouteTemplates.UploadProcessHub}");
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUri, (op) => { op.Headers.Add("X-APIKEY", _uploadOptions.APIKey); })
                    .WithAutomaticReconnect()
                    .Build();
                _hubConnection.On<UploadSessionStatusNotification>("ReceiveUploadProcessNotification", status =>
                {
                    OnUploadProgressChanged(UploadProgressChangedEventArgs.Create(status));
                });
                // TODO: Check if connection is Closed before upload chunks.
                _hubConnection.Reconnected += _hubConnection_Reconnected;
                _hubConnection.Closed += _hubConnection_Closed;

                _retryPolicy = Policy
                    .Handle<HttpRequestException>()
                    .OrResult<HttpResponseMessage>(response =>
                        !response.IsSuccessStatusCode)
                    .WaitAndRetryAsync(
                        retryCount: _uploadOptions.MaxRetriesCount,
                        sleepDurationProvider: (retryAttempt, result, _) =>
                        {
                            // Check if the response indicates TooManyRequests (429)
                            if (result.Result?.StatusCode == HttpStatusCode.TooManyRequests)
                            {
                                // Use the Retry-After header if available
                                var retryAfter = result.Result.Headers.RetryAfter;
                                if (retryAfter != null)
                                {
                                    if (retryAfter.Delta.HasValue && retryAfter.Delta.Value > TimeSpan.Zero)
                                        return retryAfter.Delta.Value;

                                    if (retryAfter.Date.HasValue)
                                    {
                                        var delay = retryAfter.Date.Value - DateTime.UtcNow;
                                        return delay > TimeSpan.Zero ? delay : TimeSpan.FromSeconds(_uploadOptions.DefaultRetryDelayInSeconds);
                                    }
                                }
                            }

                            // Default backoff for other errors
                            var backoffDelay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                            return backoffDelay > TimeSpan.Zero && backoffDelay < TimeSpan.FromSeconds(10) ? backoffDelay : TimeSpan.FromSeconds(_uploadOptions.DefaultRetryDelayInSeconds);
                        },
                        onRetryAsync: (outcome, delay, retryAttempt, _) =>
                        {

                            _logger.LogWarning(
                "Retrying due to {StatusCode}. Attempt {RetryAttempt}. Waiting {Timespan} before retrying.",
                outcome.Result?.StatusCode, retryAttempt, delay);
                            return Task.CompletedTask;
                        });

                foreach (string extension in _uploadOptions.ExcludedCompressionExtensions)
                {
                    _fileCompressor.AddExcludedExtension(extension);
                }


            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }
        #endregion Constructor
        #region Public Methods
        /// <inheritdoc />
        /// <exception cref="UploadException"></exception>
        /// <remarks>Throws an <see cref="UploadException"/> if the service is already processing an upload request.</remarks>
        public async Task UploadFileAsync(string filePath)
        {

            try
            {
                _logger.LogInformation("Starting file upload for {FilePath}", filePath);
                if (_sessionStatus != SessionStatus.None)
                {
                    _logger.LogWarning("Upload request already in progress.");
                    throw new UploadException("The service is already processing an upload request.");
                }
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    _logger.LogWarning("File path is null or whitespace.");
                    throw new ArgumentException($"'{nameof(filePath)}' cannot be null or whitespace.", nameof(filePath));
                }
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("File does not exist: {FilePath}", filePath);
                    throw new UploadException("The file does not exist");
                }
                _originalFilePath = filePath;
                CreateNewCancellationTokenSource();
                await CheckAPIHealth();

                await RunHubConnection().ConfigureAwait(false);
                await CompressFile();
                await CreateSession();
                await SplitFile();
                await StartUploading();
                await CompleteUpload().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }


        }
        /// <inheritdoc />
        public async Task PauseUploadAsync()
        {
            try
            {
                if (CanPauseSession)
                {
                    _logger.LogInformation("Pausing upload session {SessionId}", _sessionId);
                    OnSessionPausing();
                    _cancellationTokenSource.Cancel();
                    await ExecuteWithRetryPolicy(() =>
                        _httpClient.PostAsJsonAsync(RouteTemplates.PauseSession,
                            new PauseUploadSessionRequest { SessionId = _sessionId },
                            CancellationToken.None
                        )
                    );
                    OnSessionPaused(new SessionPausedEventArgs(_sessionId, Path.GetFileName(_originalFilePath), _originalFileSize));
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }
        /// <inheritdoc />
        public async Task ResumeUploadAsync()
        {
            // TODO : Check for resuming the session if an error occurred before creating the session.
            try
            {
                CreateNewCancellationTokenSource();
                if (!CanResumeSession)
                {
                    _logger.LogWarning("Cannot resume session {SessionId}", _sessionId);
                    throw new UploadException("The session cannot be resumed");
                }

                OnSessionResuming();
                if (_sessionId == Guid.Empty && _originalFilePath != null)// check first that the session been created
                {
                    await RunHubConnection();
                    OnSessionResumed(new SessionResumedEventArgs(_sessionId, Path.GetFileName(_originalFilePath), _originalFileSize));
                    if (!_fileHasBeenCompressed)
                    {
                        await CompressFile();
                    }
                    await CreateSession();
                    await SplitFile();
                    await StartUploading();
                    await CompleteUpload().ConfigureAwait(false);
                    return;

                }
                // if the session already been created and we have not compressed or split the file we need to compress and split the file and then upload the chunks
                if (!_fileHasBeenCompressed)
                {
                    await CompressFile();

                }
                if (!_fileHasBeenSplit)
                {
                    await SplitFile();
                }
                // if session already been created  check if we have been  compressed and spited the file. if so we then just need to upload remain chunks
                await UploadRemainChunks();
            }
            catch (Exception ex)
            {

                HandleException(ex);
            }
        }
        /// <inheritdoc />
        public async Task CancelUploadAsync()
        {
            try
            {
                if (CanCancelSession)
                {
                    _logger.LogInformation("Canceling upload session {SessionId}", _sessionId);
                    OnSessionCanceling();
                    _cancellationTokenSource.Cancel();
                    var completeResponse = await ExecuteWithRetryPolicy(() =>
                            _httpClient.PostAsJsonAsync(RouteTemplates.CancelSession,
                                new CancelUploadSessionRequest { SessionId = _sessionId },
                                CancellationToken.None
                            )
                        );
                    completeResponse.EnsureSuccessStatusCode();
                    _sessionStatus = SessionStatus.Canceled;
                    OnSessionCanceled(new SessionCanceledEventArgs(_sessionId, Path.GetFileName(_originalFilePath), new FileInfo(_originalFilePath).Length));
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }
        #endregion Public Methods
        #region Private Methods
        /// <summary>
        /// Handles the event when the hub connection is closed.
        /// </summary>
        /// <param name="arg">The exception that caused the connection to close, if any.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task _hubConnection_Closed(Exception? arg)
        {
            _logger.LogWarning("Hub connection closed. Exception: {Exception}", arg);
            _hubConnectionsId = null;
            await Task.CompletedTask.ConfigureAwait(false);
        }
        /// <summary>
        /// Starts the SignalR hub connection if it is not already connected.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the hub connection is null.</exception>
        private async Task RunHubConnection()

        {
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync().ConfigureAwait(false);
                _hubConnectionsId = _hubConnection.ConnectionId;
            }
        }

        /// <summary>
        /// Uploads the remaining chunks of a file if the session has already been created, compressed, and split.
        /// </summary>
        /// <remarks>
        /// This method checks if the file has been compressed and split. If so, it retrieves the upload session status
        /// and uploads any remaining chunks.<br></br> If the upload status is pending completion, it completes the upload.
        /// If the upload status is already completed, it raises the session completed event.
        /// </remarks>
        /// <exception cref="UploadException">Thrown when the upload session status cannot be retrieved or if an error occurs during the upload process.</exception>
        /// <exception cref="HttpRequestException">Thrown if the HTTP request fails.</exception>

        private async Task UploadRemainChunks()
        {
            if (_fileHasBeenSplit) // if session already been created  check if we have been spited the file. if so we then just need to upload remain chunks
            {
                _logger.LogInformation("Resuming upload session {SessionId}", _sessionId);
                var statusResponse = await ExecuteWithRetryPolicy(() => _httpClient.GetAsync($"{RouteTemplates.SessionStatus}?sessionId={_sessionId}", _cancellationTokenSource.Token));
                statusResponse.EnsureSuccessStatusCode();
                var uploadStatus = await statusResponse.Content.ReadFromJsonAsync<UploadSessionStatusResponse>().ConfigureAwait(false);
                if (uploadStatus != null)
                {
                    if (uploadStatus.RemainChunks != null && uploadStatus.RemainChunks.Any())
                    {
                        _logger.LogInformation("Uploading remaining chunks for session {SessionId}", _sessionId);
                        var remainingChunkPaths = uploadStatus.RemainChunks.Select(index => _chunksToUpload[index]).ToList();
                        var uploadTasks = remainingChunkPaths.Select((chunkPath, index) => UploadChunkWithLimitAsync(chunkPath, uploadStatus.RemainChunks[index])).ToArray();
                        OnSessionResumed(new SessionResumedEventArgs(_sessionId, Path.GetFileName(_originalFilePath), _originalFileSize));
                        await Task.WhenAll(uploadTasks).ConfigureAwait(false);

                        await CompleteUpload().ConfigureAwait(false);
                    }
                    else if (uploadStatus.UploadStatus == UploadStatus.PendingToComplete)
                    {
                        await CompleteUpload().ConfigureAwait(false);
                    }
                    else if (uploadStatus.UploadStatus == UploadStatus.Completed && !IsSessionCompleted)
                    {
                        OnSessionCompleting();
                        _sessionStatus = SessionStatus.Completed;
                        OnSessionCompleted(new SessionCompletedEventArgs(_sessionId, Path.GetFileName(_originalFilePath), new FileInfo(_originalFilePath).Length));
                    }
                    statusResponse.Dispose();
                }
                else
                {
                    _logger.LogError("Failed to get upload session status for session {SessionId}", _sessionId);
                    throw new UploadException("Failed to get upload session status");
                }
            }
        }

        /// <summary>
        /// Uploads a chunk of the file asynchronously.
        /// </summary>
        /// <param name="chunkPath">The path of the chunk to be uploaded.</param>
        /// <param name="chunkIndex">The index of the chunk.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
        /// <exception cref="HttpRequestException">Thrown if the HTTP request fails.</exception>
        private async Task UploadChunkAsync(string chunkPath, int chunkIndex)
        {

            _cancellationTokenSource.Token.ThrowIfCancellationRequested();
            _logger.LogInformation("Uploading chunk {ChunkIndex} for session {SessionId}", chunkIndex, _sessionId);
            var chunkData = await File.ReadAllBytesAsync(chunkPath, _cancellationTokenSource.Token).ConfigureAwait(false);

            var uploadChunkRequest = new UploadChunkRequest
            {
                SessionId = _sessionId,
                ChunkIndex = chunkIndex,
                ChunkData = chunkData,
                HubConnectionId = _hubConnectionsId
            };
            _cancellationTokenSource.Token.ThrowIfCancellationRequested();
            var response = await ExecuteWithRetryPolicy(() =>
                _httpClient.PostAsJsonAsync(RouteTemplates.UploadChunk, uploadChunkRequest, _cancellationTokenSource.Token)
            );
            response.EnsureSuccessStatusCode();

            OnChunkUploaded(new ChunkUploadedEventArgs(_sessionId, chunkIndex, chunkData.Length));
            response.Dispose();
            //_chunksToUpload.RemoveAt(chunkIndex);
        }
        /// <summary>
        /// Uploads a chunk of the file with concurrency limit asynchronously.
        /// </summary>
        /// <param name="chunkPath">The path of the chunk to be uploaded.</param>
        /// <param name="chunkIndex">The index of the chunk.</param>

        private async Task UploadChunkWithLimitAsync(string chunkPath, int chunkIndex)
        {
            await _semaphore.WaitAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
            try
            {
                await UploadChunkAsync(chunkPath, chunkIndex).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading chunk {ChunkIndex}", chunkIndex);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
            //Thread.Sleep(1000);
            //await UploadChunkAsync(chunkPath, chunkIndex).ConfigureAwait(false);

        }

        /// <summary>
        /// Completes the upload session by sending a request to the server.
        /// </summary>
        /// <remarks>
        /// This method checks if the session status is neither completed nor canceled before attempting to complete the upload session.
        /// If the session can be completed, it sends a request to the server to mark the session as complete.
        /// If the session is already completed or canceled, it logs a warning and throws an <see cref="UploadException"/>.
        /// </remarks>
        /// <exception cref="UploadException">Thrown when the session cannot be completed.</exception>
        /// <exception cref="HttpRequestException">Thrown if the HTTP request fails.</exception>

        private async Task CompleteUpload()
        {
            if (_sessionStatus != SessionStatus.Completed && _sessionStatus != SessionStatus.Canceled)
            {
                _logger.LogInformation("Completing upload session {SessionId}", _sessionId);
                OnSessionCompleting();
                var completeResponse = await ExecuteWithRetryPolicy(() => _httpClient.PostAsJsonAsync($"{RouteTemplates.CompleteSession}", new CompleteUploadSessionRequest() { SessionId = _sessionId }, _cancellationTokenSource.Token));
                completeResponse.EnsureSuccessStatusCode();
                OnSessionCompleted(new SessionCompletedEventArgs(_sessionId, Path.GetFileName(_originalFilePath), new FileInfo(_originalFilePath).Length));
                completeResponse.Dispose();
            }
            else
            {
                _logger.LogWarning("Cannot complete session {SessionId}", _sessionId);
                throw new UploadException("The session cannot be completed");
            }
        }

        /// <summary>
        /// Executes the provided action with a retry policy.
        /// </summary>
        /// <param name="action">The action to execute, which returns an <see cref="HttpResponseMessage"/>.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="HttpResponseMessage"/>.</returns>
        /// <exception cref="UploadException">Thrown when an error occurs during the upload process.</exception>
        private async Task<HttpResponseMessage> ExecuteWithRetryPolicy(Func<Task<HttpResponseMessage>> action)
        {
            await CheckAPIHealth();

            var response = await _retryPolicy.ExecuteAsync(action).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                // handle Authentication  errors
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    OnAuthenticationError();

                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    _logger.LogError("Error occurred during the upload process. Status code: {StatusCode}, Error message: {ErrorMessage}", response.StatusCode, errorMessage);
                    OnUploadError(errorMessage, null);
                }

            }
            return response;
        }
        /// <summary>
        /// Checks the health of the API and updates the connection status.
        /// </summary>
        /// <remarks>
        /// This method checks the health of the API at regular intervals (every 5 seconds). If the connection status is degraded,
        /// it is treated as healthy. If the connection status is unhealthy or times out, the current upload session is paused,
        /// and a network error is raised.
        /// </remarks>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task CheckAPIHealth()
        {
            if (_networkConnectionChecker != null)
            {
                if ((DateTime.Now.TimeOfDay - _lastHealthCheck) > TimeSpan.FromSeconds(5))
                {
                    _lastHealthCheck = DateTime.Now.TimeOfDay;
                    _cachedConnectionStatus = await _networkConnectionChecker.CheckApiHealthAsync().ConfigureAwait(false);
                    if (_cachedConnectionStatus == ConnectionStatus.Degraded)
                    {
                        _cachedConnectionStatus = ConnectionStatus.Healthy; // handling ToManyRequest scenario.
                    }
                    if (_cachedConnectionStatus == ConnectionStatus.Unhealthy)
                    {
                        _cancellationTokenSource.Cancel();
                        CreateNewCancellationTokenSource();
                        if (CanPauseSession)
                        {
                            OnSessionPausing();
                            OnSessionPaused(new SessionPausedEventArgs(_sessionId, Path.GetFileName(_originalFilePath), _originalFileSize));
                        }
                        OnNetworkError("The Upload Server is in UnHealthy Sate", null);
                    }
                    if (_cachedConnectionStatus == ConnectionStatus.Timeout)
                    {
                        _cancellationTokenSource.Cancel();
                        CreateNewCancellationTokenSource();
                        if (CanPauseSession)
                        {
                            OnSessionPausing();
                            OnSessionPaused(new SessionPausedEventArgs(_sessionId, Path.GetFileName(_originalFilePath), _originalFileSize));
                        }
                        OnNetworkError("Request Time out for Checking Server health", null);
                    }
                }
            }
        }
        /// <summary>
        /// Compresses the file specified in the upload options.
        /// </summary>
        /// <remarks>
        /// This method checks if the file is applicable for compression based on its extension.
        /// If applicable, it compresses the file using the specified compression algorithm and level.
        /// If the file is not applicable for compression, it  override the upload compression option to be null.
        /// </remarks>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
        private async Task CompressFile()
        {
            _cancellationTokenSource.Token.ThrowIfCancellationRequested();
            if (_uploadOptions.CompressionOption != null && _originalFilePath != null)
            {
                if (_fileCompressor.IsFileApplicableForCompression(_originalFilePath))
                {
                    _logger.LogInformation("Compressing file {FilePath}", _originalFilePath);
                    OnFileCompressionStarted();
                    _compressedFilePath = Path.Combine(_uploadOptions.TempDirectory, $"{Path.GetFileName(_originalFilePath)}.gz");
                    await _fileCompressor.CompressFileAsync(_originalFilePath,
                                                            _uploadOptions.TempDirectory,
                                                            _uploadOptions.CompressionOption.Algorithm,
                                                            _uploadOptions.CompressionOption.Level,
                                                            _cancellationTokenSource.Token).ConfigureAwait(false);
                    OnFileCompressionCompleted();
                }
                else
                {
                    _fileHasBeenCompressed = true;
                    _uploadOptions.CompressionOption = null;
                    _logger.LogInformation("Compression is not applicable for file extension {extension} we have override the upload compression option to be null", Path.GetExtension(_originalFilePath));
                }
            }
        }
        private async Task SplitFile()
        {

            _cancellationTokenSource.Token.ThrowIfCancellationRequested();
            OnFileSplittingStarted();
            string? fileToSplit = _uploadOptions.CompressionOption != null ? _compressedFilePath : _originalFilePath;

            if (fileToSplit is null)
            {
                throw new UploadException("There is no file to split");
            }
            _chunksToUpload = await _fileProcessor.SplitFileIntoChunksAsync(fileToSplit!, _chunkSize, _uploadOptions.TempDirectory, _cancellationTokenSource.Token).ConfigureAwait(false);
            if (_chunksToUpload.Count != _totalChunksToUpload)
            {
                _logger.LogError("Error in splitting the file. Expected chunks: {ExpectedChunks}, Actual chunks: {ActualChunks}", _totalChunksToUpload, _chunksToUpload.Count);
                throw new UploadException($"Error in splitting the file. Expected chunks: {_totalChunksToUpload}, Actual chunks: {_chunksToUpload.Count}");
            }
            OnFileSplittingCompleted();

        }
        private async Task StartUploading()
        {
            _cancellationTokenSource.Token.ThrowIfCancellationRequested();
            _logger.LogInformation("Uploading chunks in parallel.");
            var uploadTasks = _chunksToUpload.Select((chunkPath, index) => UploadChunkWithLimitAsync(chunkPath, index)).ToArray();
            await Task.WhenAll(uploadTasks).ConfigureAwait(false);
        }
        private async Task CreateSession()
        {
            _cancellationTokenSource.Token.ThrowIfCancellationRequested();
            if (_originalFilePath != null)
            {
                var createSessionRequest = new CreateUploadSessionRequest
                {
                    FileName = Path.GetFileName(_originalFilePath),
                    FileSize = _originalFileSize,
                    CompressedFileSize = _compressedFileSize,
                    FileExtension = Path.GetExtension(_originalFilePath),
                    Compression = _uploadOptions.CompressionOption,
                    HubConnectionId = _hubConnectionsId,
                };
                var createSessionResponse = await ExecuteWithRetryPolicy(() => _httpClient.PostAsJsonAsync(RouteTemplates.CreateSession, createSessionRequest, _cancellationTokenSource.Token));
                createSessionResponse.EnsureSuccessStatusCode();
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();
                var sessionResponse = await createSessionResponse.Content.ReadFromJsonAsync<CreateUploadSessionResponse>().ConfigureAwait(false);

                if (sessionResponse == null)
                {
                    _logger.LogError("Failed to create upload session.");
                    throw new UploadException("Failed to create upload session");
                }
                _sessionId = sessionResponse.SessionId;
                _chunkSize = sessionResponse.MaxChunkSize;
                _totalChunksToUpload = sessionResponse.TotalChunksToUpload;
                OnSessionCreated(SessionCreatedEventArgs.Create(sessionResponse));
            }


        }
        private void HandleException(Exception exception)
        {
            _logger.LogError("An error occurred during the upload process.");
            _cancellationTokenSource.Cancel();
            switch (exception)
            {
                case UploadException uploadException:
                    throw uploadException;
                case ApplicationException:
                    OnUploadError(exception.Message, exception);
                    break;
                case ArgumentException argumentException:
                    OnUploadError("An argument error occurred during the upload process. See the inner Exception for details.", argumentException);
                    break;
                case HttpRequestException httpRequestException:
                    if (httpRequestException.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        OnAuthenticationError();
                    }
                    else if (httpRequestException.HttpRequestError == HttpRequestError.ConnectionError)
                    {
                        OnNetworkError("An Connection Error occurred during the upload process. See the inner Exception for details.", httpRequestException);
                    }
                    else
                    {
                        if (httpRequestException.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            OnUploadError("To Many request please try again after a while", exception);
                            break;
                        }
                        OnUploadError("An HTTP request error occurred during the upload process.  See the inner Exception for details.", httpRequestException);
                    }
                    break;
                case TaskCanceledException taskCanceledException:
                    //  OnUploadError("The upload process was canceled.", taskCanceledException);
                    break;
                case IOException ioException:
                    OnUploadError("An I/O error occurred during the upload process. See the inner Exception for details.", ioException);
                    break;
                case UnauthorizedAccessException unauthorizedAccessException:
                    OnUploadError("Access to a file or directory was denied during the upload process. See the inner Exception for details.", unauthorizedAccessException);
                    break;
                case OperationCanceledException operationCanceledException:
                    // OnUploadError("The operation was canceled.", operationCanceledException);
                    break;
                case TimeoutException timeoutException:
                    OnUploadError("The operation timed out.", timeoutException);
                    break;
                default:
                    if (exception.HResult == -2146233088)
                    {
                        OnUploadError("To Many request please try again after a while", exception);
                        break;
                    }
                    OnUploadError("An error occurred during the upload process.", exception);
                    break;
            }
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
        private void CreateNewCancellationTokenSource()
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }
        #region Event Raisers
        /// <summary>
        /// Raises the <see cref="SessionCreated"/> event.
        /// </summary>
        /// <param name="e">The event data.</param>
        private void OnSessionCreated(SessionCreatedEventArgs e)
        {
            _sessionStatus = SessionStatus.Created;
            SessionCreated?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="ChunkUploaded"/> event.
        /// </summary>
        /// <param name="e">The event data.</param>
        private void OnChunkUploaded(ChunkUploadedEventArgs e)
        {
            _sessionStatus = SessionStatus.Uploading;
            ChunkUploaded?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="SessionCompleted"/> event.
        /// </summary>
        /// <param name="e">The event data.</param>
        private void OnSessionCompleted(SessionCompletedEventArgs e)
        {
            _sessionStatus = SessionStatus.Completed;
            SessionCompleted?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="SessionCanceled"/> event.
        /// </summary>
        /// <param name="e">The event data.</param>
        private void OnSessionCanceled(SessionCanceledEventArgs e)
        {
            _sessionStatus = SessionStatus.Canceled;
            SessionCanceled?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="SessionPaused"/> event.
        /// </summary>
        /// <param name="e">The event data.</param>
        private void OnSessionPaused(SessionPausedEventArgs e)
        {
            _sessionStatus = SessionStatus.Paused;
            SessionPaused?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="SessionResumed"/> event.
        /// </summary>
        /// <param name="e">The event data.</param>
        private void OnSessionResumed(SessionResumedEventArgs e)
        {
            SessionResumed?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="UploadProgressChanged"/> event.
        /// </summary>
        /// <param name="e">The event data.</param>
        private void OnUploadProgressChanged(UploadProgressChangedEventArgs e)
        {
            UploadProgressChanged?.Invoke(this, e);
        }

        private void OnSessionPausing()
        {
            SessionPausing?.Invoke(this, EventArgs.Empty);
        }
        private void OnSessionResuming()
        {
            SessionResuming?.Invoke(this, EventArgs.Empty);
        }
        private void OnSessionCanceling()
        {
            SessionCanceling?.Invoke(this, EventArgs.Empty);
        }
        private void OnSessionCompleting()
        {
            SessionCompleting?.Invoke(this, EventArgs.Empty);
        }
        private void OnFileSplittingStarted()
        {
            FileSplittingStarted?.Invoke(this, EventArgs.Empty);
        }
        private void OnFileSplittingCompleted()
        {
            _fileHasBeenSplit = true;
            FileSplittingCompleted?.Invoke(this, EventArgs.Empty);
        }
        private void OnFileCompressionStarted()
        {
            FileCompressionStarted?.Invoke(this, EventArgs.Empty);
        }
        private void OnFileCompressionCompleted()
        {
            _fileHasBeenCompressed = true;
            FileCompressionCompleted?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Raises the <see cref="UploadError"/> event.
        /// </summary>
        /// <param name="message">The error message.</param>
        private void OnUploadError(string message, Exception? exception)
        {
            UploadError?.Invoke(this, message);
            throw new UploadException(message, exception);
        }
        private void OnNetworkError(string message, Exception? exception)
        {
            NetworkError?.Invoke(this, message);
            throw new UploadException(message, exception);
        }
        private void OnAuthenticationError()
        {
            AuthenticationError?.Invoke(this, "Authentication is required");
            throw new UploadException("Authentication error occurred.");
        }
        #endregion

        #endregion Private methods
        #region IDisposable Implementation
        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="FileUploadService"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    _httpClient.Dispose();
                    _semaphore.Dispose();
                    _hubConnection.DisposeAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                    DeleteTempFiles();
                }
            }
        }
        /// <inheritdoc/>
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
            try
            {
                if (_compressedFilePath != null)
                {
                    File.Delete(_compressedFilePath);
                    _compressedFilePath = null;
                }
                if (_chunksToUpload != null)
                {
                    foreach (var chunkPath in _chunksToUpload)
                    {
                        File.Delete(chunkPath);
                    }
                    _chunksToUpload.Clear();
                }
                _originalFilePath = null;
            }
            catch (Exception)
            {


            }

        }
        #endregion
    }
}