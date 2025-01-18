using AdvanceFileUpload.Domain.Core;
using AdvanceFileUpload.Domain.Events;
using AdvanceFileUpload.Domain.Exception;
using AdvanceFileUpload.Domain.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Domain
{
    /// <summary>
    /// Represents a file upload session.
    /// </summary>
    public class FileUploadSession : EntityBase, IAggregateRoot
    {
        /// <summary>
        /// Gets the name of the file being uploaded.
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Gets the file extension of the file being uploaded.
        /// </summary>
        public string FileExtension { get { return Path.GetExtension(FileName); } }

        /// <summary>
        /// Gets the directory where the file is being saved.
        /// </summary>
        public string SavingDirectory { get; private set; }

        /// <summary>
        /// Gets the size of the file being uploaded.
        /// </summary>
        public long FileSize { get; private set; }

        /// <summary>
        /// Gets or sets the status of the file upload session.
        /// </summary>
        public FileUploadSessionStatus Status { get; set; }

        /// <summary>
        /// Gets the upload date of the file.
        /// </summary>
        public DateOnly UploadDate { get => SessionStartDate.ToDateOnly(); }

        /// <summary>
        /// Gets the start date of the session.
        /// </summary>
        public DateTime SessionStartDate { get; private set; }

        /// <summary>
        /// Gets or sets the end date of the session.
        /// </summary>
        public DateTime? SessionEndDate { get; set; }

        /// <summary>
        /// Gets the maximum size of each chunk.
        /// </summary>
        public long MaxChunkSize { get; private set; }

        /// <summary>
        /// Gets the total number of chunks to be uploaded.
        /// </summary>
        public int TotalChunksToUpload { get => (int)Math.Ceiling((double)FileSize / MaxChunkSize); }

        /// <summary>
        /// Gets the total number of chunks that have been uploaded.
        /// </summary>
        public int TotalUploadedChunks { get => _chunkFiles.Count; }

        /// <summary>
        /// Gets the progress percentage of the file upload.
        /// </summary>
        public double ProgressPercentage
        {
            get
            {
                if (TotalChunksToUpload <= 0)
                {
                    return 0;
                }
                return (double)(TotalUploadedChunks / TotalChunksToUpload) * 100;
            }
        }

        private readonly List<ChunkFile> _chunkFiles = new();
        /// <summary>
        /// Gets the collection of chunk files.
        /// </summary>
        public IReadOnlyCollection<ChunkFile> ChunkFiles => _chunkFiles.AsReadOnly();

        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadSession"/> class.
        /// </summary>
        /// <param name="fileName">The name of the file being uploaded.</param>
        /// <param name="savingDirectory">The directory where the file is being saved.</param>
        /// <param name="fileSize">The size of the file being uploaded.</param>
        /// <param name="maxChunkSize">The maximum size of each chunk.</param>
        /// <exception cref="ArgumentException">Thrown when any of the parameters are invalid.</exception>
        public FileUploadSession(string fileName, string savingDirectory, long fileSize, long maxChunkSize) : base()
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("The file name must be specified.", nameof(fileName));
            }
            if (string.IsNullOrWhiteSpace(savingDirectory))
            {
                throw new ArgumentException("The saving directory must be specified.", nameof(savingDirectory));
            }
            if (!Directory.Exists(savingDirectory))
            {
                Directory.CreateDirectory(savingDirectory);
            }
            if (fileSize <= 0)
            {
                throw new ArgumentException("The file size must be >0.", nameof(fileSize));
            }
            if (maxChunkSize <= 0)
            {
                throw new ArgumentException("The max chunk size must be >0.", nameof(maxChunkSize));
            }
            FileName = fileName;
            SavingDirectory = savingDirectory;
            FileSize = fileSize;
            MaxChunkSize = maxChunkSize;
            Status = FileUploadSessionStatus.InProgress;
            SessionStartDate = DateTime.Now;
            this.AddDomainEvent(new FileUploadSessionStartedEvent(this));
        }

        /// <summary>
        /// Adds a chunk to the file upload session.
        /// </summary>
        /// <param name="chunkIndex">The index of the chunk.</param>
        /// <param name="chunkPath">The path of the chunk.</param>
        /// <exception cref="ChunkUploadingException">Thrown when the chunk cannot be added.</exception>
        public void AddChunk(int chunkIndex, string chunkPath)
        {
            if (IsCompleted())
            {
                throw new ChunkUploadingException("The Upload Session already Completed");
            }
            if (IsCanceled())
            {
                throw new ChunkUploadingException("The Upload Session already been Canceled");
            }
            if (IsAllChunkUploaded())
            {
                throw new ChunkUploadingException("All chunks already Uploaded");
            }
            if (IsChunkUploaded(chunkIndex))
            {
                throw new ChunkUploadingException($"The chunk with index {chunkIndex} already uploaded.");
            }
            ChunkFile chunkFile = new ChunkFile(this.Id, chunkIndex, chunkPath);
            _chunkFiles.Add(chunkFile);
            Status = FileUploadSessionStatus.InProgress;
            this.AddDomainEvent(new ChunkUploadedEvent(chunkFile));
        }

        /// <summary>
        /// Determines whether all chunks have been uploaded.
        /// </summary>
        /// <returns><c>true</c> if all chunks have been uploaded; otherwise, <c>false</c>.</returns>
        public bool IsAllChunkUploaded()
        {
            return TotalChunksToUpload == TotalUploadedChunks;
        }

        /// <summary>
        /// Determines whether a specific chunk has been uploaded.
        /// </summary>
        /// <param name="chunkIndex">The index of the chunk.</param>
        /// <returns><c>true</c> if the chunk has been uploaded; otherwise, <c>false</c>.</returns>
        public bool IsChunkUploaded(int chunkIndex)
        {
            return _chunkFiles.Any(c => c.ChunkIndex == chunkIndex);
        }

        /// <summary>
        /// Determines whether the file upload session is completed.
        /// </summary>
        /// <returns><c>true</c> if the session is completed; otherwise, <c>false</c>.</returns>
        public bool IsCompleted()
        {
            return Status == FileUploadSessionStatus.Completed;
        }

        /// <summary>
        /// Determines whether the file upload session is canceled.
        /// </summary>
        /// <returns><c>true</c> if the session is canceled; otherwise, <c>false</c>.</returns>
        public bool IsCanceled()
        {
            return Status == FileUploadSessionStatus.Canceled;
        }

        #region Manage Status

        /// <summary>
        /// Cancels the file upload session.
        /// </summary>
        /// <exception cref="CancelationFileUploadException">Thrown when the session cannot be canceled.</exception>
        public void CancelSession()
        {
            if (Status == FileUploadSessionStatus.Completed)
            {
                throw new CancelationFileUploadException("The Upload Session already Completed");
            }
            Status = FileUploadSessionStatus.Canceled;
            SessionEndDate = DateTime.Now;
            this.AddDomainEvent(new FileUploadSessionCanceledEvent(this));
        }

        /// <summary>
        /// Pauses the file upload session.
        /// </summary>
        /// <exception cref="CancelationFileUploadException">Thrown when the session cannot be paused.</exception>
        public void PauseSession()
        {
            if (Status == FileUploadSessionStatus.Completed)
            {
                throw new CancelationFileUploadException("The Upload Session already Completed");
            }
            if (Status == FileUploadSessionStatus.Canceled)
            {
                throw new CancelationFileUploadException("The Upload Session already Canceled");
            }
            Status = FileUploadSessionStatus.Paused;
            SessionEndDate = DateTime.Now;
            this.AddDomainEvent(new FileUploadSessionPausedEvent(this));
        }

        /// <summary>
        /// Completes the file upload session.
        /// </summary>
        /// <exception cref="CompletingFileUploadException">Thrown when the session cannot be completed.</exception>
        public void CompleteSession()
        {
            if (IsCompleted())
            {
                throw new CompletingFileUploadException("The Upload Session already completed");
            }
            if (!IsAllChunkUploaded())
            {
                throw new CompletingFileUploadException("All chunks must be uploaded before completing the session.");
            }
            Status = FileUploadSessionStatus.Completed;
            SessionEndDate = DateTime.Now;
            this.AddDomainEvent(new FileUploadSessionCompletedEvent(this));
        }

        #endregion Manage Status
    }
}
