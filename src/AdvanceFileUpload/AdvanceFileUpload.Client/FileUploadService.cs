using AdvanceFileUpload.Application.Compression;
using AdvanceFileUpload.Application.FileProcessing;
using AdvanceFileUpload.Application.Request;
using AdvanceFileUpload.Application.Response;
using AdvanceFileUpload.Application.Shared;
using AdvanceFileUpload.Client.HttpExtensions;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AdvanceFileUpload.Client
{
    public class FileUploadService : IFileUploadService, IDisposable
    {
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

        private long _compressedFileSize => _compressedFilePath is null ? 0 : new FileInfo(_compressedFilePath).Length;

        public event EventHandler<SessionCreatedEventArgs> SessionCreated;
        public event EventHandler<ChunkUploadedEventArgs> ChunkUploaded;
        public event EventHandler<SessionCompletedEventArgs> SessionCompleted;
        public event EventHandler<SessionCanceledEventArgs> SessionCanceled;
        public event EventHandler<SessionPausedEventArgs> SessionPaused;
        public event EventHandler<SessionResumedEventArgs> SessionResumed;
        public event EventHandler<UploadProgressChangedEventArgs> UploadProgressChanged;

        public FileUploadService(Uri apiBaseAddress, UploadOptions uploadOptions)
        {
            _httpClient = new HttpClient()
            {
                BaseAddress = apiBaseAddress ?? throw new ArgumentNullException(nameof(apiBaseAddress))
            };
            _uploadOptions = uploadOptions ?? throw new ArgumentNullException(nameof(uploadOptions));
            _semaphore = new SemaphoreSlim(_uploadOptions.MaxConcurrentUploads, _uploadOptions.MaxConcurrentUploads);
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{apiBaseAddress}/{RouteTemplates.UploadProcessHub}")
                .WithAutomaticReconnect()
                .Build();
            _hubConnection.On<UploadSessionStatusNotification>("ReceiveUploadProcessNotification", status =>
            {
                OnUploadProgressChanged(UploadProgressChangedEventArgs.Create(status));
            });
            _hubConnection.Reconnected += _hubConnection_Reconnected;
        }
        public async Task UploadFileAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or whitespace.", nameof(filePath));
            }
            if (!File.Exists(filePath))
            {
                throw new ApplicationException("The file does not exist");
            }

            _cancellationTokenSource = new CancellationTokenSource();
            await _hubConnection.StartAsync(_cancellationTokenSource.Token).ConfigureAwait(false);

            // Compress the file if compression is enabled
            if (_uploadOptions.CompressionOption != null)
            {
                _compressedFilePath = Path.Combine(_uploadOptions.TempDirectory, $"{Path.GetFileNameWithoutExtension(filePath)}.compressed");
                await _fileCompressor.CompressFileAsync(filePath, _uploadOptions.TempDirectory, _uploadOptions.CompressionOption.Algorithm, _uploadOptions.CompressionOption.Level, _cancellationTokenSource.Token).ConfigureAwait(false);
                filePath = _compressedFilePath;
            }

            // Create upload session
            var createSessionRequest = new CreateUploadSessionRequest
            {
                FileName = Path.GetFileName(filePath),
                FileSize = new FileInfo(filePath).Length,
                FileExtension = Path.GetExtension(filePath),
                Compression = _uploadOptions.CompressionOption,
                HubConnectionId=_hubConnectionsId,
                
            };

            var createSessionResponse = await _httpClient.PostAsJsonAsync(RouteTemplates.CreateSession, createSessionRequest, _cancellationTokenSource.Token).ConfigureAwait(false);
            createSessionResponse.EnsureSuccessStatusCode();
            var sessionResponse = await createSessionResponse.Content.ReadFromJsonAsync<CreateUploadSessionResponse>().ConfigureAwait(false);

            if (sessionResponse == null)
            {
                throw new ApplicationException("Failed to create upload session");
            }

            _sessionId = sessionResponse.SessionId;
            _chunkSize = sessionResponse.MaxChunkSize;
            _totalChunksToUpload = sessionResponse.TotalChunksToUpload;
            _originalFilePath = filePath;

            OnSessionCreated(SessionCreatedEventArgs.Create(sessionResponse));

            // Split file into chunks
            _chunksToUpload = await _fileProcessor.SplitFileIntoChunksAsync(filePath, _chunkSize, _uploadOptions.TempDirectory, _cancellationTokenSource.Token).ConfigureAwait(false);

            // Upload chunks in parallel
            var uploadTasks = _chunksToUpload.Select((chunkPath, index) => UploadChunkWithLimitAsync(chunkPath, index, _cancellationTokenSource.Token)).ToArray();
            await Task.WhenAll(uploadTasks).ConfigureAwait(false);

            // Complete upload session
            await CompleteUpload().ConfigureAwait(false);
        }

        private async Task UploadChunkAsync(string chunkPath, int chunkIndex, CancellationToken cancellationToken)
        {
            var chunkData = await File.ReadAllBytesAsync(chunkPath, cancellationToken).ConfigureAwait(false);
            var uploadChunkRequest = new UploadChunkRequest
            {
                SessionId = _sessionId,
                ChunkIndex = chunkIndex,
                ChunkData = chunkData,
                HubConnectionId = _hubConnectionsId
            };
            var response = await RunWithRetryPolicy(() => _httpClient.PostAsJsonAsync(RouteTemplates.UploadChunk, uploadChunkRequest, cancellationToken), _uploadOptions.MaxRetriesCount).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            OnChunkUploaded(new ChunkUploadedEventArgs(_sessionId, chunkIndex, chunkData.Length));
        }

        public async Task PauseUploadAsync()
        {
            if (_sessionStatus == SessionStatus.Uploading)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
                await _httpClient.PostAsync($"{RouteTemplates.PauseSession}{_sessionId}", null, _cancellationTokenSource.Token).ConfigureAwait(false);
                OnSessionPaused(new SessionPausedEventArgs(_sessionId, Path.GetFileName(_originalFilePath), _originalFileSize));
            }
            else
            {
                throw new ApplicationException("The session cannot be paused");
            }
        }

        public async Task ResumeUploadAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            OnSessionResumed(new SessionResumedEventArgs(_sessionId, Path.GetFileName(_originalFilePath), _originalFileSize));

            // Get the status of the upload session
            var statusResponse = await _httpClient.GetFromJsonAsync<UploadSessionStatusResponse>($"{RouteTemplates.SessionStatus}{_sessionId}", _cancellationTokenSource.Token).ConfigureAwait(false);
            if (statusResponse != null)
            {
                if (statusResponse.RemainChunks != null && statusResponse.RemainChunks.Any())
                {
                    // Upload remaining chunks in parallel
                    var remainingChunkPaths = statusResponse.RemainChunks.Select(index => _chunksToUpload[index]).ToList();
                    var uploadTasks = remainingChunkPaths.Select((chunkPath, index) => UploadChunkWithLimitAsync(chunkPath, statusResponse.RemainChunks[index], _cancellationTokenSource.Token)).ToArray();
                    await Task.WhenAll(uploadTasks).ConfigureAwait(false);

                    // Complete upload session
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
                throw new ApplicationException("Failed to get upload session status");
            }
        }

        public async Task CancelUploadAsync()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            var completeResponse = await RunWithRetryPolicy(() => _httpClient.PostAsync($"{RouteTemplates.CancelSession}{_sessionId}", null, _cancellationTokenSource.Token), _uploadOptions.MaxRetriesCount).ConfigureAwait(false);
            completeResponse.EnsureSuccessStatusCode();
            OnSessionCanceled(new SessionCanceledEventArgs(_sessionId, Path.GetFileName(_originalFilePath), new FileInfo(_originalFilePath).Length));
        }

        private async Task UploadChunkWithLimitAsync(string chunkPath, int chunkIndex, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await UploadChunkAsync(chunkPath, chunkIndex, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task CompleteUpload()
        {
            if (_sessionStatus != SessionStatus.Completed && _sessionStatus != SessionStatus.Canceled)
            {
                var completeResponse = await RunWithRetryPolicy(() => _httpClient.PostAsync($"{RouteTemplates.CompleteSession}{_sessionId}", null, _cancellationTokenSource.Token), _uploadOptions.MaxRetriesCount).ConfigureAwait(false);
                completeResponse.EnsureSuccessStatusCode();
                OnSessionCompleted(new SessionCompletedEventArgs(_sessionId, Path.GetFileName(_originalFilePath), new FileInfo(_originalFilePath).Length));
            }
            else
            {
                throw new ApplicationException("The session cannot be completed");
            }
        }

        private static async Task<HttpResponseMessage> RunWithRetryPolicy(Func<Task<HttpResponseMessage>> action, int maxRetries = 3, int delayMilliseconds = 2000)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    var response = await action().ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        return response;
                    }
                }
                catch (HttpRequestException) { }

                retryCount++;
                if (retryCount >= maxRetries)
                {
                    throw new ApplicationException("Maximum retry attempts exceeded.");
                }

                await Task.Delay(delayMilliseconds * retryCount).ConfigureAwait(false);
            }
        }
        private async Task _hubConnection_Reconnected(string? arg)
        {
            _hubConnectionsId = arg;
            await Task.CompletedTask.ConfigureAwait(false);
        }

        protected virtual void OnSessionCreated(SessionCreatedEventArgs e)
        {
            _sessionStatus = SessionStatus.Created;
            SessionCreated?.Invoke(this, e);
        }

        protected virtual void OnChunkUploaded(ChunkUploadedEventArgs e)
        {
            _sessionStatus = SessionStatus.Uploading;
            ChunkUploaded?.Invoke(this, e);
        }

        protected virtual void OnSessionCompleted(SessionCompletedEventArgs e)
        {
            _sessionStatus = SessionStatus.Completed;
            SessionCompleted?.Invoke(this, e);
        }

        protected virtual void OnSessionCanceled(SessionCanceledEventArgs e)
        {
            _sessionStatus = SessionStatus.Canceled;
            SessionCanceled?.Invoke(this, e);
        }

        protected virtual void OnSessionPaused(SessionPausedEventArgs e)
        {
            _sessionStatus = SessionStatus.Paused;
            SessionPaused?.Invoke(this, e);
        }

        protected virtual void OnSessionResumed(SessionResumedEventArgs e)
        {
            SessionResumed?.Invoke(this, e);
        }

        protected virtual void OnUploadProgressChanged(UploadProgressChangedEventArgs e)
        {
            UploadProgressChanged?.Invoke(this, e);
        }

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

    }
}
