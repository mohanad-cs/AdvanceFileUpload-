using AdvanceFileUpload.Domain.Events;
using MediatR;

namespace AdvanceFileUpload.Application.EventHandling
{
    //TODO: Implement the functionality of publishing to RabbitMQ
    public sealed class FileUploadSessionCreatedEventHandler : INotificationHandler<FileUploadSessionCreatedEvent>
    {
        public async Task Handle(FileUploadSessionCreatedEvent notification, CancellationToken cancellationToken)
        {
          await  Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
