using AdvanceFileUpload.Domain.Core;

namespace AdvanceFileUpload.Domain.Events
{
    /// <summary>
    /// Event that is triggered when a file upload session is completed.
    /// </summary>
    public sealed class FileUploadSessionCompletedEvent : DomainEventBase
    {
        /// <summary>
        /// Gets the file upload session associated with this event.
        /// </summary>
        public FileUploadSession FileUploadSession { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadSessionCompletedEvent"/> class.
        /// </summary>
        /// <param name="fileUploadSession">The file upload session that has been completed.</param>
        /// <exception cref="ArgumentNullException">Thrown when the file upload session is null.</exception>
        public FileUploadSessionCompletedEvent(FileUploadSession fileUploadSession) : base()
        {
            FileUploadSession = fileUploadSession ?? throw new ArgumentNullException(nameof(fileUploadSession));
        }
    }
}
