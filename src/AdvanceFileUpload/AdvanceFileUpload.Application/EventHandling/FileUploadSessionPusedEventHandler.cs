using AdvanceFileUpload.Domain.Events;
using MediatR;

namespace AdvanceFileUpload.Application.EventHandling
{
    //TODO: Implement the functionality of publishing to RabbitMQ
    public sealed class FileUploadSessionPusedEventHandler : INotificationHandler<FileUploadSessionPausedEvent>
    {
        public Task Handle(FileUploadSessionPausedEvent notification, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
