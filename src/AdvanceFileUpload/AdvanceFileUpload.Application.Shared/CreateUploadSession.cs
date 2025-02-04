using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Application.Shared
{



    /// <summary>
    ///  Represents a request to create a new file upload session.
    /// </summary>
    public sealed class CreateUploadSessionRequest
    {
        /// <summary>
        /// Gets the name of the file to be uploaded.
        /// </summary>
        public required string FileName { get; init; }

        /// <summary>
        /// Gets the size of the file to be uploaded.
        /// </summary>
        public long FileSize { get; init; }

        /// <summary>
        /// Gets the file extension of the file to be uploaded.
        /// </summary>
        public required string FileExtension { get; init; }
    }

    /// <summary>
    /// Represents the response after creating a new file upload session.
    /// </summary>
    public sealed class CreateUploadSessionResponse
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
        public long MaxMaxChunkSize { get; init; }

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
    }
    /// <summary>
    /// Represents the status response of an upload session.
    /// </summary>
    public sealed class UploadSessionStatusResponse
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
        public long MaxMaxChunkSize { get; init; }

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
    }
    /// <summary>
    /// Represents a request to upload a chunk of a file.
    /// </summary>
    public sealed class UploadChunkRequest
    {
        /// <summary>
        /// Gets the unique identifier of the upload session.
        /// </summary>
        public Guid SessionId { get; init; }

        /// <summary>
        /// Gets the index of the chunk being uploaded.
        /// </summary>
        public int ChunkIndex { get; init; }

        /// <summary>
        /// Gets the data of the chunk being uploaded.
        /// </summary>
        public required byte[] ChunkData { get; init; }
    }


    /// <summary>
    /// Represents the status of an upload session.
    /// </summary>
    public enum UploadStatus
    {
        /// <summary>
        /// The upload session is pending.
        /// </summary>
        Pending,

        /// <summary>
        /// The upload session is in progress.
        /// </summary>
        InProgress,

        /// <summary>
        /// The upload session is completed.
        /// </summary>
        Completed,

        /// <summary>
        /// The upload session has failed.
        /// </summary>
        Failed
    }
}
