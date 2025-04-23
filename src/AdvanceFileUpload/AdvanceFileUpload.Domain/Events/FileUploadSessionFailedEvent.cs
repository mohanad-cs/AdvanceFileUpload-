using AdvanceFileUpload.Domain.Core;

namespace AdvanceFileUpload.Domain.Events
{
    public sealed class FileUploadSessionFailedEvent : DomainEventBase 
    { 
        public FileUploadSession FileUploadSession { get; }
        public FileUploadSessionFailedEvent(FileUploadSession fileUploadSession) : base()
        {
            FileUploadSession = fileUploadSession ?? throw new ArgumentNullException(nameof(fileUploadSession));
        }
    }
    
}
