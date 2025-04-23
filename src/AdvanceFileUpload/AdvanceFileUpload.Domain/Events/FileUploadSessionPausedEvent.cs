using AdvanceFileUpload.Domain.Core;

namespace AdvanceFileUpload.Domain.Events
{
    /// <summary>
    /// Event that is triggered when a file upload session is paused.
    /// </summary>
    public sealed class FileUploadSessionPausedEvent : DomainEventBase
    {
        /// <summary>
        /// Gets the file upload session associated with this event.
        /// </summary>
        public FileUploadSession FileUploadSession { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadSessionPausedEvent"/> class.
        /// </summary>
        /// <param name="fileUploadSession">The file upload session that has been paused.</param>
        /// <exception cref="ArgumentNullException">Thrown when the file upload session is null.</exception>
        public FileUploadSessionPausedEvent(FileUploadSession fileUploadSession) : base()
        {
            FileUploadSession = fileUploadSession ?? throw new ArgumentNullException(nameof(fileUploadSession));
        }
    }
    
}
