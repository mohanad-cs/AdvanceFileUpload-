using AdvanceFileUpload.Domain.Core;
using AdvanceFileUpload.Domain.Events;
using AdvanceFileUpload.Domain.Extensions;

namespace AdvanceFileUpload.Domain
{
    /// <summary>
    /// Represents a file upload session.
    /// </summary>
    public class FileUploadSession : EntityBase, IAggregateRoot
    {
        private readonly List<ChunkFile> _chunkFiles = new();

        /// <summary>
        /// Gets the name of the file being uploaded.
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Gets the file extension of the file being uploaded.
        /// </summary>
        public string FileExtension { get; private set; }

        /// <summary>
        /// Gets the compression Algorithm for the file being uploaded.
        /// </summary>
        public CompressionAlgorithm? CompressionAlgorithm { get; private set; }
        /// <summary>
        /// Gets the compression Level for the file being uploaded.
        /// </summary>
        public CompressionLevel? CompressionLevel { get; private set; }

        /// <summary>
        /// Gets the directory where the file is being saved.
        /// </summary>
        public string SavingDirectory { get; private set; }

        /// <summary>
        /// Gets the size of the file being uploaded.
        /// </summary>
        public long FileSize { get; private set; }
        /// <summary>
        /// Gets the size of the file after compression.
        /// </summary>
        /// <remarks>if the <see cref="CompressedFileSize"/> is not null.<br></br> 
        /// The <see cref="CompressedFileSize"/> will be used to figure the <see cref="TotalChunksToUpload"/> <br></br>
        /// else <see cref="FileSize"/> will be used.
        /// </remarks>
        public long? CompressedFileSize { get; private set; }

        /// <summary>
        /// Gets the status of the file upload session.
        /// </summary>
        public FileUploadSessionStatus Status { get; private set; }

        /// <summary>
        /// Gets the upload date of the file.
        /// </summary>
        public DateOnly UploadDate { get; private set; }

        /// <summary>
        /// Gets the start date of the session.
        /// </summary>
        public DateTime SessionStartDate { get; private set; }

        /// <summary>
        /// Gets the end date of the session.
        /// </summary>
        public DateTime? SessionEndDate { get; private set; }

        /// <summary>
        /// Gets the maximum size of each chunk.
        /// </summary>
        public long MaxChunkSize { get; private set; }

        /// <summary>
        /// Gets the total number of chunks to be uploaded.
        /// </summary>
        public int TotalChunksToUpload { get => (int)Math.Ceiling((double)(CompressedFileSize ?? FileSize) / MaxChunkSize); }

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
                return ((double)TotalUploadedChunks / TotalChunksToUpload) * 100;
            }
        }
        /// <summary>
        /// Gets a value indicating whether compression is used.
        /// </summary>
        public bool UseCompression => CompressionAlgorithm != null;
        /// <summary>
        /// Gets or set the current hub connection id.
        /// </summary>
        public string? CurrentHubConnectionId { get; set; }
        /// <summary>
        ///  The Row Version.
        /// </summary>
        public byte[] Version { get; set; }

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
        /// <param name="compressedSize">The size of the compressed file being uploaded.</param>
        /// <param name="maxChunkSize">The maximum size of each chunk.</param>
        /// <param name="compressionAlgorithm">The compression Algorithm for the file being uploaded.</param>
        /// <param name="compressionLevel">The compression level for the file being uploaded.</param>
        public FileUploadSession(string fileName, string savingDirectory, long fileSize, long? compressedSize, long maxChunkSize, CompressionAlgorithm? compressionAlgorithm = null, CompressionLevel? compressionLevel = null) : base()
        {
            ValidateParameters(fileName, savingDirectory, fileSize, compressedSize, maxChunkSize);
            InitializeProperties(fileName, savingDirectory, fileSize, compressedSize, maxChunkSize, compressionAlgorithm, compressionLevel);
            AddDomainEvent(new FileUploadSessionCreatedEvent(this));
        }
        private FileUploadSession()
        {

        }

        /// <summary>
        /// Validates the parameters for the file upload session.
        /// </summary>
        /// <param name="fileName">The name of the file being uploaded.</param>
        /// <param name="savingDirectory">The directory where the file is being saved.</param>
        /// <param name="fileSize">The size of the file being uploaded.</param>
        /// <param name="compressedSize">The size of the file if compressed.</param>
        /// <param name="maxChunkSize">The maximum size of each chunk.</param>
        /// <exception cref="ArgumentException">Thrown when any of the parameters are invalid.</exception>
        private void ValidateParameters(string fileName, string savingDirectory, long fileSize, long? compressedSize, long maxChunkSize)
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
        }

        /// <summary>
        /// Initializes the properties for the file upload session.
        /// </summary>
        /// <param name="fileName">The name of the file being uploaded.</param>
        /// <param name="savingDirectory">The directory where the file is being saved.</param>
        /// <param name="fileSize">The size of the file being uploaded.</param>
        /// <param name="compressedSize">The size of the file if compressed.</param>
        /// <param name="maxChunkSize">The maximum size of each chunk.</param>
        /// <param name="compressionAlgorithm">The compression Algorithm for the file being uploaded.</param>
        /// <param name="compressionLevel">The compression level for the file being uploaded.</param>
        private void InitializeProperties(string fileName, string savingDirectory, long fileSize, long? compressedSize, long maxChunkSize, CompressionAlgorithm? compressionAlgorithm, CompressionLevel? compressionLevel)
        {
            if ((compressedSize == null || compressedSize == 0) && compressionAlgorithm != null)
            {
                throw new DomainException("The Compressed file size should not be null or equal to 0 when using compression");

            }
            FileName = fileName;
            SavingDirectory = savingDirectory;
            FileSize = fileSize;
            MaxChunkSize = maxChunkSize;
            Status = FileUploadSessionStatus.InProgress;
            SessionStartDate = DateTime.Now;
            FileExtension = Path.GetExtension(FileName);
            UploadDate = SessionStartDate.ToDateOnly();

            CompressionAlgorithm = compressionAlgorithm;
            CompressionLevel = compressionLevel;
            CompressedFileSize = CompressionAlgorithm.HasValue ? compressedSize : null;
        }

        /// <summary>
        /// Adds a chunk to the file upload session.
        /// </summary>
        /// <param name="chunkIndex">The index of the chunk.</param>
        /// <param name="chunkPath">The path of the chunk.</param>
        /// <exception cref="DomainException">
        /// Thrown when the chunk cannot be added due to one of the following reasons:
        /// <list type="bullet">
        /// <item><description>The upload session is already completed.</description></item>
        /// <item><description>The upload session is already canceled.</description></item>
        /// <item><description>All chunks are already uploaded.</description></item>
        /// <item><description>The chunk with the specified index is already uploaded.</description></item>
        /// </list>
        /// </exception>
        public void AddChunk(int chunkIndex, string chunkPath)
        {
            if (IsCompleted())
            {
                throw new DomainException("The Upload Session already Completed");
            }
            if (IsCanceled())
            {
                throw new DomainException("The Upload Session already been Canceled");
            }
            if (IsAllChunkUploaded())
            {
                throw new DomainException("All chunks already Uploaded");
            }
            if (IsChunkUploaded(chunkIndex))
            {
                throw new DomainException($"The chunk with index {chunkIndex} already uploaded.");
            }
            ChunkFile chunkFile = new ChunkFile(this.Id, chunkIndex, chunkPath);
            _chunkFiles.Add(chunkFile);
            if (IsAllChunkUploaded())
            {
                Status = FileUploadSessionStatus.PendingToComplete;
            }
            else
            {
                Status = FileUploadSessionStatus.InProgress;
            }

            AddDomainEvent(new ChunkUploadedEvent(chunkFile));
        }

        /// <summary>
        /// Gets the remaining chunks to be uploaded.
        /// </summary>
        /// <returns>
        ///  <see cref="List{T}"/> of the remaining chunks to be uploaded.
        /// </returns>
        public List<int> GetRemainChunks()
        {
            List<int> remainChunks = new();
            for (int i = 0; i < TotalChunksToUpload; i++)
            {
                if (!IsChunkUploaded(i))
                {
                    remainChunks.Add(i);
                }
            }
            return remainChunks;
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
        /// <summary>
        ///  Determines whether the file upload session is failed.
        /// </summary>
        /// <returns><c>true</c> if the session status is Failed; otherwise, <c>false</c>.</returns>
        public bool IsFailed()
        {
            return Status == FileUploadSessionStatus.Failed;
        }
        /// <summary>
        /// Cancels the file upload session.
        /// </summary>
        /// <exception cref="DomainException">
        /// Thrown when the session cannot be canceled because it is already completed or failed.
        /// </exception>
        public void CancelSession()
        {
            if (IsFailed())
            {
                throw new DomainException("The upload session can not be Canceled because it is in failed status");
            }
            if (IsCompleted())
            {
                throw new DomainException("The Upload Session already Completed");
            }
            Status = FileUploadSessionStatus.Canceled;
            SessionEndDate = DateTime.Now;
            this.AddDomainEvent(new FileUploadSessionCanceledEvent(this));
        }

        /// <summary>
        /// Pauses the file upload session.
        /// </summary>
        /// <exception cref="DomainException">
        /// Thrown when the session cannot be paused because it is already completed, canceled  or failed.
        /// </exception>
        public void PauseSession()
        {
            if (IsFailed())
            {
                throw new DomainException("The upload session can not be paused because it is in failed status");
            }
            if (IsCompleted())
            {
                throw new DomainException("The Upload Session already Completed");
            }
            if (IsCanceled())
            {
                throw new DomainException("The Upload Session already Canceled");
            }
            Status = FileUploadSessionStatus.Paused;
            SessionEndDate = DateTime.Now;
            this.AddDomainEvent(new FileUploadSessionPausedEvent(this));
        }

        /// <summary>
        /// Completes the file upload session.
        /// </summary>
        /// <exception cref="DomainException">
        /// Thrown when the session cannot be completed due to one of the following reasons:
        /// <list type="bullet">
        /// <item><description>The session is already completed.</description></item>
        /// <item><description>The session is canceled.</description></item>
        ///  <item><description>The session is failed.</description></item>
        /// <item><description>Not all chunks have been uploaded.</description></item>
        /// </list>
        /// </exception>
        public void CompleteSession()
        {
            if (IsFailed())
            {
                throw new DomainException("The upload session can not be completed because it is in failed status");
            }
            if (IsCompleted())
            {
                throw new DomainException("The Upload Session already completed");
            }
            if (IsCanceled())
            {
                throw new DomainException("The Upload Session is been Canceled");
            }
            if (!IsAllChunkUploaded())
            {
                throw new DomainException($"All chunks must be uploaded before completing the session. Total Chunks To Upload:({TotalChunksToUpload}), Remaining Chunks Count:({GetRemainChunks().Count}) ");
            }
            Status = FileUploadSessionStatus.Completed;
            SessionEndDate = DateTime.Now;
            this.AddDomainEvent(new FileUploadSessionCompletedEvent(this));
        }
       
        ///<summary>
        /// Marks the file upload session as failed.
        /// </summary>
        /// <remarks>
        /// This method updates the status of the session to <see cref="FileUploadSessionStatus.Failed"/> and sets the <see cref="SessionEndDate"/> to the current date and time.
        /// It also raises a <see cref="FileUploadSessionFailedEvent"/> to notify that the session has been failed.
        /// </remarks>
        /// <exception cref="DomainException">
        /// Thrown when the session cannot be marked as failed due to one of the following reasons:
        /// <list type="bullet">
        /// <item><description>The session is already completed.</description></item>
        /// <item><description>The session is canceled.</description></item>
        /// </list>
        /// </exception>
        public void MarkAsFailed()
        {
            if (IsFailed())
            {
                return;
            }
            if (IsCompleted())
            {
                throw new DomainException("The Upload Session already completed");
            }
            if (IsCanceled())
            {
                throw new DomainException("The Upload Session is been Canceled");
            }
            Status = FileUploadSessionStatus.Failed;
            SessionEndDate = DateTime.Now;
            this.AddDomainEvent(new FileUploadSessionFailedEvent(this));
        }
    }
}
