using AdvanceFileUpload.Domain.Events;
using MediatR;

namespace AdvanceFileUpload.Application.EventHandling
{
    //TODO: Implement the functionality of publishing to RabbitMQ
    public sealed class FileUploadSessionCanceledEventHandler : INotificationHandler<FileUploadSessionCanceledEvent>
    {
        public async Task Handle(FileUploadSessionCanceledEvent notification, CancellationToken cancellationToken)
        {
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }
}
