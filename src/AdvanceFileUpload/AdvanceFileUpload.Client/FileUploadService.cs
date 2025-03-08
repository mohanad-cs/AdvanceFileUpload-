using AdvanceFileUpload.Application.Compression;
using AdvanceFileUpload.Application.FileProcessing;
using AdvanceFileUpload.Application.Request;
using AdvanceFileUpload.Application.Response;
using AdvanceFileUpload.Application.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Client
{
    public class FileUploadService
    {
        private readonly HttpClient _httpClient;
        private readonly IFileProcessor _fileProcessor;
        private readonly IFileCompressor _fileCompressor;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly UploadOptions _uploadOptions;
        private List<string> _chunksToUpload = [];
        private Guid _sessionId;
        private long _chunkSize;
        private int _totalChunksToUpload;
        private long _originalFileSize;

        public event EventHandler<SessionCreatedEventArgs> SessionCreated;
        public event EventHandler<ChunkUploadedEventArgs> ChunkUploaded;
        public event EventHandler<SessionCompletedEventArgs> SessionCompleted;
        public event EventHandler<SessionCanceledEventArgs> SessionCanceled;
        public event EventHandler<SessionPausedEventArgs> SessionPaused;
        public event EventHandler<SessionResumedEventArgs> SessionResumed;
        public event EventHandler<UploadProgressChangedEventArgs> UploadProgressChanged;


        public FileUploadService(HttpClient httpClient,IFileProcessor fileProcessor , IFileCompressor fileCompressor, UploadOptions uploadOptions, CancellationToken cancellationToken)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this._fileProcessor = fileProcessor;
            this._fileCompressor = fileCompressor;
            _uploadOptions = uploadOptions ?? throw new ArgumentNullException(nameof(uploadOptions));
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        public async Task UploadFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException($"'{nameof(filePath)}' cannot be null or whitespace.", nameof(filePath));
            }
            if (!File.Exists(filePath))
            {
                throw new ApplicationException("the file do not exist");
            }
            var fileInfo = new FileInfo(filePath);
            CreateUploadSessionRequest requestBody = new() { FileName = fileInfo.Name, FileExtension = fileInfo.Extension, FileSize = fileInfo.Length, Compression = _uploadOptions.CompressionOption };

            var createUploadSessionHttpResponse = await _httpClient.PostAsJsonAsync<CreateUploadSessionRequest>(RouteTemplates.CreateSession, requestBody, _cancellationTokenSource.Token);
            if (createUploadSessionHttpResponse != null)
            {
                createUploadSessionHttpResponse.EnsureSuccessStatusCode();
                var createUploadSessionResponse = await createUploadSessionHttpResponse.Content.ReadFromJsonAsync<CreateUploadSessionResponse>();
                OnSessionCreated(SessionCreatedEventArgs.Create(createUploadSessionResponse));

                // start Upload Chunks
                var chunkSize = createUploadSessionResponse.MaxChunkSize;
                var totalChunks = createUploadSessionResponse.TotalChunksToUpload;
                if (_uploadOptions.CompressionOption!=null)
                {
                    await _fileCompressor.CompressFileAsync(filePath, _uploadOptions.TempDirectory, _uploadOptions.CompressionOption.Algorithm, _uploadOptions.CompressionOption.Level, _cancellationTokenSource.Token);
                    string compressedFilePath = Path.Combine(_uploadOptions.TempDirectory, $"{fileInfo.Name}.gz");
                  List<string> chunks=  await _fileProcessor.SplitFileIntoChunksAsync(compressedFilePath, chunkSize, _uploadOptions.TempDirectory, _cancellationTokenSource.Token);
                    foreach (var chunk in chunks)
                    {
                        var chunkIndex = chunks.IndexOf(chunk);
                        byte[] chunkData = await File.ReadAllBytesAsync(chunk, _cancellationTokenSource.Token);
                        var uploadChunkRequest = new UploadChunkRequest() { SessionId = createUploadSessionResponse.SessionId,ChunkIndex = chunkIndex,ChunkData=chunkData};
                        var uploadChunkHttpResponse = await _httpClient.PostAsJsonAsync<UploadChunkRequest>(RouteTemplates.UploadChunk,uploadChunkRequest, _cancellationTokenSource.Token);
                        if (uploadChunkHttpResponse != null)
                        {
                            uploadChunkHttpResponse.EnsureSuccessStatusCode();
                            OnChunkUploaded(new ChunkUploadedEventArgs(createUploadSessionResponse.SessionId, chunkIndex, chunkSize));
                        }
                    }
                  var completeSessionHttpResponse= await _httpClient.PostAsJsonAsync(RouteTemplates.CompleteSession,createUploadSessionResponse.SessionId, _cancellationTokenSource.Token);
                    if (completeSessionHttpResponse != null)
                    {
                        completeSessionHttpResponse.EnsureSuccessStatusCode();
                        OnSessionCompleted(new SessionCompletedEventArgs(createUploadSessionResponse.SessionId,fileInfo.Name, createUploadSessionResponse.FileSize));

                    }
                }
                else
                {
                    List<string> chunks = await _fileProcessor.SplitFileIntoChunksAsync(filePath, chunkSize, _uploadOptions.TempDirectory, _cancellationTokenSource.Token);
                    foreach (var chunk in chunks)
                    {
                        var chunkIndex = chunks.IndexOf(chunk);
                        byte[] chunkData = await File.ReadAllBytesAsync(chunk, _cancellationTokenSource.Token);
                        var uploadChunkRequest = new UploadChunkRequest() { SessionId = createUploadSessionResponse.SessionId, ChunkIndex = chunkIndex, ChunkData = chunkData };
                        var uploadChunkHttpResponse = await _httpClient.PostAsJsonAsync<UploadChunkRequest>(RouteTemplates.UploadChunk, uploadChunkRequest, _cancellationTokenSource.Token);
                        if (uploadChunkHttpResponse != null)
                        {
                            uploadChunkHttpResponse.EnsureSuccessStatusCode();
                            OnChunkUploaded(new ChunkUploadedEventArgs(createUploadSessionResponse.SessionId, chunkIndex, chunkSize));
                        }
                    }
                    var completeSessionHttpResponse = await _httpClient.PostAsJsonAsync(RouteTemplates.CompleteSession, createUploadSessionResponse.SessionId, _cancellationTokenSource.Token);
                    if (completeSessionHttpResponse != null)
                    {
                        completeSessionHttpResponse.EnsureSuccessStatusCode();
                        OnSessionCompleted(new SessionCompletedEventArgs(createUploadSessionResponse.SessionId, fileInfo.Name, createUploadSessionResponse.FileSize));

                    }
                }
            }

        }

        public async Task PauseUpload()
        {
            await _cancellationTokenSource.CancelAsync();
        }
        public async Task ResumeUpload()
        {
            throw new NotImplementedException();
        }

        protected virtual void OnSessionCreated(SessionCreatedEventArgs e)
        {
            SessionCreated?.Invoke(this, e);
        }
        protected virtual void OnChunkUploaded(ChunkUploadedEventArgs e)
        {
            ChunkUploaded?.Invoke(this, e);
        }
        protected virtual void OnSessionCompleted(SessionCompletedEventArgs e)
        {
            SessionCompleted?.Invoke(this, e);
        }
        protected virtual void OnSessionCanceled(SessionCanceledEventArgs e)
        {
            SessionCanceled?.Invoke(this, e);
        }
        protected virtual void OnSessionPaused(SessionPausedEventArgs e)
        {
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

    }

    public class UploadOptions
    {
        public CompressionOption? CompressionOption { get; set; }
        public string TempDirectory { get; set; }

    }
}
