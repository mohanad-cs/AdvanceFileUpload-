using AdvanceFileUpload.Application.Compression;
using AdvanceFileUpload.Application.FileProcessing;
using AdvanceFileUpload.Application.Request;
using AdvanceFileUpload.Application.Response;
using AdvanceFileUpload.Application.Shared;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Client
{
    public class FileUploadService : IFileUploadService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly IFileProcessor _fileProcessor;
        private readonly IFileCompressor _fileCompressor;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly UploadOptions _uploadOptions;
        private List<string> _chunksToUpload = [];
        private Guid _sessionId;
        private long _chunkSize;
        private int _totalChunksToUpload;
        private string? _originalFilePath;
        private SessionStatus _sessionStatus = SessionStatus.None;
        private SemaphoreSlim _semaphore;
        private readonly HubConnection _hubConnection;


        private long _originalFileSize
        {
            get
            {
                if (_originalFilePath is null)
                {
                    return 0;
                }
                return new FileInfo(_originalFilePath).Length;
            }
        }
        private string? _compressedFilePath;
        private bool disposedValue;

        private long _compressedFileSize
        {
            get
            {
                if (_compressedFilePath is null)
                {
                    return 0;
                }
                return new FileInfo(_compressedFilePath).Length;
            }
        }

        public event EventHandler<SessionCreatedEventArgs> SessionCreated;
        public event EventHandler<ChunkUploadedEventArgs> ChunkUploaded;
        public event EventHandler<SessionCompletedEventArgs> SessionCompleted;
        public event EventHandler<SessionCanceledEventArgs> SessionCanceled;
        public event EventHandler<SessionPausedEventArgs> SessionPaused;
        public event EventHandler<SessionResumedEventArgs> SessionResumed;
        public event EventHandler<UploadProgressChangedEventArgs> UploadProgressChanged;


        public FileUploadService(Uri apiBaseAddress, UploadOptions uploadOptions)
        {
            if (apiBaseAddress is null)
            {
                throw new ArgumentNullException(nameof(apiBaseAddress));
            }

            if (uploadOptions is null)
            {
                throw new ArgumentNullException(nameof(uploadOptions));
            }
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = apiBaseAddress;
            _uploadOptions = uploadOptions;
            _semaphore = new SemaphoreSlim(_uploadOptions.MaxConcurrentUploads, _uploadOptions.MaxConcurrentUploads);
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{apiBaseAddress}/{RouteTemplates.UploadProcessHub}")
                .WithAutomaticReconnect()
                .Build();
            _hubConnection.On<UploadSessionStatusNotification>("ReceiveUploadProcessNotification", (status) =>
            {
                OnUploadProgressChanged(UploadProgressChangedEventArgs.Create(status));
            });
            _hubConnection.Reconnected += _hubConnection_Reconnected;
           
            
        }

        private async Task _hubConnection_Reconnected(string? arg)
        {
           await Task.FromResult( ()=>_httpClient.DefaultRequestHeaders.Add("HubConnectionId", arg));
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

            // Compress the file if compression is enabled
            if (_uploadOptions.CompressionOption != null)
            {
                _compressedFilePath = Path.Combine(_uploadOptions.TempDirectory, $"{Path.GetFileNameWithoutExtension(filePath)}.compressed");
                await _fileCompressor.CompressFileAsync(filePath, _uploadOptions.TempDirectory, _uploadOptions.CompressionOption.Algorithm, _uploadOptions.CompressionOption.Level, _cancellationTokenSource.Token);
                filePath = _compressedFilePath;
            }

            // Create upload session
            var createSessionRequest = new CreateUploadSessionRequest
            {
                FileName = Path.GetFileName(filePath),
                FileSize = new FileInfo(filePath).Length,
                FileExtension = Path.GetExtension(filePath),
                Compression = _uploadOptions.CompressionOption
            };

            var createSessionResponse = await _httpClient.PostAsJsonAsync(RouteTemplates.CreateSession, createSessionRequest, _cancellationTokenSource.Token);
            createSessionResponse.EnsureSuccessStatusCode();
            var sessionResponse = await createSessionResponse.Content.ReadFromJsonAsync<CreateUploadSessionResponse>();

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
            _chunksToUpload = await _fileProcessor.SplitFileIntoChunksAsync(filePath, _chunkSize, _uploadOptions.TempDirectory, _cancellationTokenSource.Token);

            // Upload chunks in parallel
            var uploadTasks = _chunksToUpload.Select((chunkPath, index) => UploadChunkWithLimitAsync(chunkPath, index, _cancellationTokenSource.Token)).ToArray();
            await Task.WhenAll(uploadTasks);

            // Complete upload session
            await CompleteUpload();
        }

        private async Task UploadChunkAsync(string chunkPath, int chunkIndex, CancellationToken cancellationToken)
        {
            var chunkData = await File.ReadAllBytesAsync(chunkPath, cancellationToken);
            var uploadChunkRequest = new UploadChunkRequest
            {
                SessionId = _sessionId,
                ChunkIndex = chunkIndex,
                ChunkData = chunkData
            };

            var response = await RunWithRetryPolicy(() => _httpClient.PostAsJsonAsync(RouteTemplates.UploadChunk, uploadChunkRequest, cancellationToken), _uploadOptions.MaxRetriesCount);
            response.EnsureSuccessStatusCode();

            OnChunkUploaded(new ChunkUploadedEventArgs(_sessionId, chunkIndex, chunkData.Length));
        }

        public async Task PauseUploadAsync()
        {
            if (_sessionStatus == SessionStatus.Uploading)
            {
                await _cancellationTokenSource.CancelAsync();
                _cancellationTokenSource = new CancellationTokenSource();
                await _httpClient.PostAsync($"{RouteTemplates.PauseSession}{_sessionId}", null, _cancellationTokenSource.Token);
                OnSessionPaused(new SessionPausedEventArgs(_sessionId, Path.GetFileName(_originalFilePath), _originalFileSize));
            }
            else
            {
                throw new ApplicationException("The session can not be Paused");
            }

        }

        public async Task ResumeUploadAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            OnSessionResumed(new SessionResumedEventArgs(_sessionId, Path.GetFileName(_originalFilePath), _originalFileSize));

            // Get the status of the upload session
            var statusResponse = await _httpClient.GetFromJsonAsync<UploadSessionStatusResponse>($"{RouteTemplates.SessionStatus}{_sessionId}", _cancellationTokenSource.Token);
            if (statusResponse != null)
            {
                if (statusResponse.RemainChunks != null && statusResponse.RemainChunks.Any())
                {

                    // Upload remaining chunks in parallel
                    var remainingChunkPaths = statusResponse.RemainChunks.Select(index => _chunksToUpload[index]).ToList();
                    var uploadTasks = remainingChunkPaths.Select((chunkPath, index) => UploadChunkWithLimitAsync(chunkPath, statusResponse.RemainChunks[index], _cancellationTokenSource.Token)).ToArray();
                    await Task.WhenAll(uploadTasks);

                    // Complete upload session
                    await CompleteUpload();
                }
                else if (statusResponse.UploadStatus == UploadStatus.PendingToComplete)
                {
                    await CompleteUpload();
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
            await _cancellationTokenSource.CancelAsync();
            _cancellationTokenSource = new CancellationTokenSource();
            var completeResponse = await RunWithRetryPolicy(() => _httpClient.PostAsync($"{RouteTemplates.CancelSession}{_sessionId}", null, _cancellationTokenSource.Token), _uploadOptions.MaxRetriesCount);
            completeResponse.EnsureSuccessStatusCode();
            OnSessionCanceled(new SessionCanceledEventArgs(_sessionId, Path.GetFileName(_originalFilePath), new FileInfo(_originalFilePath).Length));
        }

        private async Task UploadChunkWithLimitAsync(string chunkPath, int chunkIndex, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                await UploadChunkAsync(chunkPath, chunkIndex, cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        private async Task CompleteUpload()
        {
            if (_sessionStatus != SessionStatus.Completed || _sessionStatus != SessionStatus.Canceled)
            {
                var completeResponse = await RunWithRetryPolicy(() => _httpClient.PostAsync($"{RouteTemplates.CompleteSession}{_sessionId}", null, _cancellationTokenSource.Token), _uploadOptions.MaxRetriesCount);
                completeResponse.EnsureSuccessStatusCode();
                OnSessionCompleted(new SessionCompletedEventArgs(_sessionId, Path.GetFileName(_originalFilePath), new FileInfo(_originalFilePath).Length));
            }
            else
            {
                throw new ApplicationException("The session can not be Compleated");
            }

        }

        private static async Task<HttpResponseMessage> RunWithRetryPolicy(Func<Task<HttpResponseMessage>> action, int maxRetries = 3, int delayMilliseconds = 2000)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    var response = await action();
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

                await Task.Delay(delayMilliseconds * retryCount);
            }
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
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~FileUploadService()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


    }
}
