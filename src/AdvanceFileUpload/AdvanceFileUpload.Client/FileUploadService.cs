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
using System;

namespace AdvanceFileUpload.Client
{
    /// <summary>
    /// Provides functionality to upload files with support for compression, chunking, and session management.
    /// </summary>
    /// <remarks>Note that the <see cref="FileUploadService"/> have been build to process a single upload request.<br></br>
    /// if you want to upload multiple files at the same time, you need to create a new instance of the <see cref="FileUploadService"/> for each file.</remarks>
    public sealed class FileUploadService : IFileUploadService, IDisposable
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
        private readonly SemaphoreSlim _semaphore;
        private readonly HubConnection _hubConnection;
        private long _originalFileSize => _originalFilePath is null ? 0 : new FileInfo(_originalFilePath).Length;
        private string? _compressedFilePath;
        private bool _disposed;
        private string? _hubConnectionsId;
        private long? _compressedFileSize => _compressedFilePath is null ? null : new FileInfo(_compressedFilePath).Length;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private readonly ILogger<FileUploadService> _logger;
        private readonly object _statusLock = new();
        private readonly object _chunkLock = new();
        private SessionStatus sessionStatus;
        private SessionStatus _sessionStatus
        {
            get { lock (_statusLock) return sessionStatus; }
            set { lock (_statusLock) sessionStatus = value; }
        }
        private ConnectionStatus _cachedConnectionStatus;
        private readonly INetworkConnectionChecker _networkConnectionChecker;
        private TimeSpan _lastHealthCheck;
        private bool _fileHasBeenCompressed;
        private bool _fileHasBeenSplit;
        #endregion

        #region Properties
        /// <inheritdoc />
        public bool CanPauseSession => _sessionStatus == SessionStatus.Uploading || _sessionStatus == SessionStatus.None;
        /// <inheritdoc />
        public bool CanCancelSession => _sessionStatus != SessionStatus.Completed && _sessionStatus != SessionStatus.Canceled && _sessionId!=Guid.Empty;
        /// <inheritdoc />
        public bool CanResumeSession => _sessionStatus == SessionStatus.Paused || _sessionStatus == SessionStatus.None;
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
        /// <summary>
        /// Occurs when an upload error happens.
        /// </summary>
        public event EventHandler<string> UploadError;
        /// <summary>
        /// Occurs when a network error happens.
        /// </summary>
        public event EventHandler<string> NetworkError;
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
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadService"/> class.
        /// </summary>
        /// <param name="apiBaseAddress">The base address of the API.</param>
        /// <param name="uploadOptions">The upload options.</param>
        public FileUploadService(Uri apiBaseAddress, UploadOptions uploadOptions)
        {
            try
            {
                _sessionStatus = SessionStatus.None;
                _logger = LoggerFactoryHelper.CreateLogger<FileUploadService>();
                _httpClient = new HttpClient()
                {
                    BaseAddress = apiBaseAddress ?? throw new ArgumentNullException(nameof(apiBaseAddress)),
                    Timeout = TimeSpan.FromMinutes(10)


                };
                _uploadOptions = uploadOptions ?? throw new ArgumentNullException(nameof(uploadOptions));
                _semaphore = new SemaphoreSlim(_uploadOptions.MaxConcurrentUploads, _uploadOptions.MaxConcurrentUploads);
                _fileProcessor = new FileProcessor(LoggerFactoryHelper.CreateLogger<FileProcessor>());
                _fileCompressor = new FileCompressor(LoggerFactoryHelper.CreateLogger<FileCompressor>());
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
                if (_uploadOptions.CompressionOption!=null)
                {
                    foreach (string extension in _uploadOptions.CompressionOption.ExcludedCompressionExtensions)
                    {
                        _fileCompressor.AddExcludedExtension(extension);
                    }
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
                    await _cancellationTokenSource.CancelAsync();
                    await ExecuteWithRetryPolicy(ct =>
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

            //else
            //{
            //    _logger.LogWarning("Cannot pause session {SessionId}", _sessionId);
            //    throw new UploadException("The session cannot be paused");
            //}
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
                    await _cancellationTokenSource.CancelAsync();
                    var completeResponse = await ExecuteWithRetryPolicy(ct =>
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
        private async Task _hubConnection_Closed(Exception? arg)
        {
            _logger.LogWarning("Hub connection closed. Exception: {Exception}", arg);
            _hubConnectionsId = null;
            await Task.CompletedTask.ConfigureAwait(false);
        }
        private async Task RunHubConnection()
        {
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Disconnected)
            {

                await _hubConnection.StartAsync().ConfigureAwait(false);
                _hubConnectionsId = _hubConnection.ConnectionId;


            }
        }
        private async Task UploadRemainChunks()
        {
            if (_fileHasBeenCompressed && _fileHasBeenSplit) // if session already been created  check if we have been  compressed and spited the file. if so we then just need to upload remain chunks
            {
                _logger.LogInformation("Resuming upload session {SessionId}", _sessionId);
                var statusResponse = await ExecuteWithRetryPolicy(ct => _httpClient.GetAsync($"{RouteTemplates.SessionStatus}?sessionId={_sessionId}", _cancellationTokenSource.Token));
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
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
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
            var response = await ExecuteWithRetryPolicy((ct) =>
                _httpClient.PostAsJsonAsync(RouteTemplates.UploadChunk, uploadChunkRequest, _cancellationTokenSource.Token)
            );
            response.EnsureSuccessStatusCode();

            OnChunkUploaded(new ChunkUploadedEventArgs(_sessionId, chunkIndex, chunkData.Length));
            //_chunksToUpload.RemoveAt(chunkIndex);


        }
        /// <summary>
        /// Uploads a chunk of the file with concurrency limit asynchronously.
        /// </summary>
        /// <param name="chunkPath">The path of the chunk to be uploaded.</param>
        /// <param name="chunkIndex">The index of the chunk.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
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
        /// Completes the upload session asynchronously.
        /// </summary>
        private async Task CompleteUpload()
        {
            if (_sessionStatus != SessionStatus.Completed && _sessionStatus != SessionStatus.Canceled)
            {

                _logger.LogInformation("Completing upload session {SessionId}", _sessionId);
                OnSessionCompleting();
                var completeResponse = await ExecuteWithRetryPolicy((ct) => _httpClient.PostAsJsonAsync($"{RouteTemplates.CompleteSession}", new CompleteUploadSessionRequest() { SessionId = _sessionId }, _cancellationTokenSource.Token));
                completeResponse.EnsureSuccessStatusCode();
                OnSessionCompleted(new SessionCompletedEventArgs(_sessionId, Path.GetFileName(_originalFilePath), new FileInfo(_originalFilePath).Length));
            }
            else
            {
                _logger.LogWarning("Cannot complete session {SessionId}", _sessionId);
                throw new UploadException("The session cannot be completed");
            }


        }
        /// <summary>
        /// Executes an HTTP request with a retry policy.
        /// </summary>
        /// <param name="action">The HTTP request action to execute.</param>
        /// <returns>The HTTP response message.</returns>
        private async Task<HttpResponseMessage> ExecuteWithRetryPolicy(Func<CancellationToken, Task<HttpResponseMessage>> action)
        {
            await CheckAPIHealth();

            var response = await _retryPolicy.ExecuteAsync(action, _cancellationTokenSource.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                // TODO: Handle specific status codes if needed such as ToManyRequests. 
                var errorMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogError("Error occurred during the upload process. Status code: {StatusCode}, Error message: {ErrorMessage}", response.StatusCode, errorMessage);
                OnUploadError(errorMessage, null);
            }
            return response;
        }
        private async Task CheckAPIHealth()
        {

            if (_networkConnectionChecker != null)
            {
                if ((DateTime.Now.TimeOfDay - _lastHealthCheck) > TimeSpan.FromSeconds(5))
                {
                    _lastHealthCheck = DateTime.Now.TimeOfDay;
                    _cachedConnectionStatus = await _networkConnectionChecker.CheckApiHealthAsync(_cancellationTokenSource.Token).ConfigureAwait(false);
                    if (_cachedConnectionStatus == ConnectionStatus.Degraded)
                    {
                        _cachedConnectionStatus = ConnectionStatus.Healthy;// handling ToManyRequest scenario.
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
                    if (_cachedConnectionStatus== ConnectionStatus.Timeout)
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
                var createSessionResponse = await ExecuteWithRetryPolicy((ct) => _httpClient.PostAsJsonAsync(RouteTemplates.CreateSession, createSessionRequest, _cancellationTokenSource.Token));
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
                    if (httpRequestException.HttpRequestError == HttpRequestError.ConnectionError)
                    {
                        OnNetworkError("An Connection Error occurred during the upload process. See the inner Exception for details.", httpRequestException);
                    }
                    else
                    {
                        OnUploadError("An HTTP request error occurred during the upload process.  See the inner Exception for details.", httpRequestException);
                    }
                    break;
                case TaskCanceledException taskCanceledException:
                    OnUploadError("The upload process was canceled.", taskCanceledException);
                    break;
                case IOException ioException:
                    OnUploadError("An I/O error occurred during the upload process. See the inner Exception for details.", ioException);
                    break;
                case UnauthorizedAccessException unauthorizedAccessException:
                    OnUploadError("Access to a file or directory was denied during the upload process. See the inner Exception for details.", unauthorizedAccessException);
                    break;
                case OperationCanceledException operationCanceledException:
                    OnUploadError("The operation was canceled.", operationCanceledException);
                    break;
                case TimeoutException timeoutException:
                    OnUploadError("The operation timed out.", timeoutException);
                    break;
                default:
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
        #endregion
    }
}


