using AdvanceFileUpload.Domain.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Domain.Events
{
    /// <summary>
    /// Event that is triggered when a file upload session Created.
    /// </summary>
    public sealed class FileUploadSessionCreatedEvent : DomainEventBase
    {
        /// <summary>
        /// Gets the file upload session associated with this event.
        /// </summary>
        public FileUploadSession FileUploadSession { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadSessionCreatedEvent"/> class.
        /// </summary>
        /// <param name="fileUploadSession">The file upload session that has Created.</param>
        /// <exception cref="ArgumentNullException">Thrown when the file upload session is null.</exception>
        public FileUploadSessionCreatedEvent(FileUploadSession fileUploadSession) : base()
        {
            FileUploadSession = fileUploadSession ?? throw new ArgumentNullException(nameof(fileUploadSession));
        }
    }
}
