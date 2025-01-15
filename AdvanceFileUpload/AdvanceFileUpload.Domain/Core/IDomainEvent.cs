using MediatR;

namespace AdvanceFileUpload.Domain.Core
{
    public interface IDomainEvent: INotification
    {
        Guid Id { get; }
        DateTime OccurredOn { get; }
    }
}
