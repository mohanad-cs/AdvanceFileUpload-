using AdvanceFileUpload.Domain.Core;

namespace AdvanceFileUpload.Domain.Events
{
    /// <summary>
    /// Event that is triggered when a file upload session is canceled.
    /// </summary>
    public sealed class FileUploadSessionCanceledEvent : DomainEventBase
    {
        /// <summary>
        /// Gets the file upload session associated with this event.
        /// </summary>
        public FileUploadSession FileUploadSession { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadSessionCanceledEvent"/> class.
        /// </summary>
        /// <param name="fileUploadSession">The file upload session that has been canceled.</param>
        /// <exception cref="ArgumentNullException">Thrown when the file upload session is null.</exception>
        public FileUploadSessionCanceledEvent(FileUploadSession fileUploadSession) : base()
        {
            FileUploadSession = fileUploadSession ?? throw new ArgumentNullException(nameof(fileUploadSession));
        }
    }
}
