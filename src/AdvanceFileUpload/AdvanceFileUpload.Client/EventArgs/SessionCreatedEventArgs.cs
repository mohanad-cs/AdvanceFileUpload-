using AdvanceFileUpload.Application.Response;

namespace AdvanceFileUpload.Client
{
    public class SessionCreatedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the unique identifier of the upload session.
        /// </summary>
        public Guid SessionId { get; init; }

        /// <summary>
        /// Gets the size of the file to be uploaded.
        /// </summary>
        public long FileSize { get; init; }

        /// <summary>
        /// Gets the maximum size of each chunk.
        /// </summary>
        public long MaxChunkSize { get; init; }

        /// <summary>
        /// Gets the total number of chunks to be uploaded.
        /// </summary>
        public int TotalChunksToUpload { get; init; }

        /// <summary>
        /// Gets the current status of the upload session.
        /// </summary>
        public UploadStatus UploadStatus { get; init; }

        /// <summary>
        /// Gets the start date and time of the upload session.
        /// </summary>
        public DateTime SessionStartDate { get; init; }



        public static SessionCreatedEventArgs Create(CreateUploadSessionResponse createUploadSessionResponse)
        {
            return new SessionCreatedEventArgs() { FileSize = createUploadSessionResponse.FileSize, MaxChunkSize = createUploadSessionResponse.MaxChunkSize, SessionId = createUploadSessionResponse.SessionId, SessionStartDate = createUploadSessionResponse.SessionStartDate, TotalChunksToUpload = createUploadSessionResponse.TotalChunksToUpload, UploadStatus = createUploadSessionResponse.UploadStatus };
        }
    }

    public class ChunkUploadedEventArgs : EventArgs
    {
        public Guid SessionId { get; }
        public int ChunkIndex { get; }
        public long ChunkSize { get; }

        public ChunkUploadedEventArgs(Guid sessionId, int chunkIndex, long chunkSize)
        {
            SessionId = sessionId;
            ChunkIndex = chunkIndex;
            ChunkSize = chunkSize;
        }
    }
    public class SessionCompletedEventArgs : EventArgs
    {
        public Guid SessionId { get; }
        public string FileName { get; }
        public long FileSize { get; }

        public SessionCompletedEventArgs(Guid sessionId, string fileName, long fileSize)
        {
            SessionId = sessionId;
            FileName = fileName;
            FileSize = fileSize;
        }
    }
    public class SessionCanceledEventArgs : EventArgs
    {
        public Guid SessionId { get; }
        public string FileName { get; }
        public long FileSize { get; }
        public SessionCanceledEventArgs(Guid sessionId, string fileName, long fileSize)
        {
            SessionId = sessionId;
            FileName = fileName;
            FileSize = fileSize;
        }
    }
    public class SessionPausedEventArgs : EventArgs
    {
        public Guid SessionId { get; }
        public string FileName { get; }
        public long FileSize { get; }

        public SessionPausedEventArgs(Guid sessionId, string fileName, long fileSize)
        {
            SessionId = sessionId;
            FileName = fileName;
            FileSize = fileSize;
        }

    }
    public class SessionResumedEventArgs : EventArgs
    {
        public Guid SessionId { get; }
        public string FileName { get; }
        public long FileSize { get; }
        public SessionResumedEventArgs(Guid sessionId, string fileName, long fileSize)
        {
            SessionId = sessionId;
            FileName = fileName;
            FileSize = fileSize;
        }
    }
    public class UploadProgressChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the unique identifier of the upload session.
        /// </summary>
        public Guid SessionId { get; init; }

        /// <summary>
        /// Gets the size of the file to be uploaded.
        /// </summary>
        public long FileSize { get; init; }

        /// <summary>
        /// Gets the maximum size of each chunk.
        /// </summary>
        public long MaxChunkSize { get; init; }

        /// <summary>
        /// Gets the total number of chunks to be uploaded.
        /// </summary>
        public int TotalChunksToUpload { get; init; }

        /// <summary>
        /// Gets the current status of the upload session.
        /// </summary>
        public UploadStatus UploadStatus { get; init; }

        /// <summary>
        /// Gets the start date and time of the upload session.
        /// </summary>
        public DateTime SessionStartDate { get; init; }

        /// <summary>
        /// Gets the total number of chunks that have been uploaded.
        /// </summary>
        public int TotalUploadedChunks { get; init; }

        /// <summary>
        /// Gets the progress percentage of the file upload.
        /// </summary>
        public double ProgressPercentage { get; init; }

        /// <summary>
        /// Gets the end date and time of the upload session, if it has ended.
        /// </summary>
        public DateTime? SessionEndDate { get; init; }

        /// <summary>
        /// Gets the list of remaining chunks to be uploaded.
        /// </summary>
        public List<int>? RemainChunks { get; init; }

        private UploadProgressChangedEventArgs()
        {
        }
        public static UploadProgressChangedEventArgs Create(UploadSessionStatusNotification uploadSessionStatusNotification)
        {
            return new UploadProgressChangedEventArgs()
            {
                FileSize = uploadSessionStatusNotification.FileSize,
                MaxChunkSize = uploadSessionStatusNotification.MaxChunkSize,
                SessionId = uploadSessionStatusNotification.SessionId,
                SessionStartDate = uploadSessionStatusNotification.SessionStartDate,
                TotalChunksToUpload = uploadSessionStatusNotification.TotalChunksToUpload,
                UploadStatus = uploadSessionStatusNotification.UploadStatus,
                ProgressPercentage = uploadSessionStatusNotification.ProgressPercentage,
                TotalUploadedChunks = uploadSessionStatusNotification.TotalUploadedChunks,
                SessionEndDate = uploadSessionStatusNotification.SessionEndDate,
                RemainChunks = uploadSessionStatusNotification.RemainChunks,

            };
        }
    }
}
