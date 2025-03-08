using AdvanceFileUpload.Application.Request;
using AdvanceFileUpload.Application.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public Guid SessionId { get; }
        public string FileName { get; }
        public long FileSize { get; }
        public long ProgressPercentage { get; }

        public UploadProgressChangedEventArgs(Guid sessionId, string fileName, long fileSize, long progressPercentage)
        {
            SessionId = sessionId;
            FileName = fileName;
            FileSize = fileSize;
            ProgressPercentage = progressPercentage;
        }
    }
}
