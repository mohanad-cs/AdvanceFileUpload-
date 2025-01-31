namespace AdvanceFileUpload.Domain.Core
{
    public interface IDomainEventPublisher
    {
        Task PublishAsync(IDomainEvent domainEvent ,CancellationToken cancellationToken=default);
        Task PublishAsync(IEnumerable<IDomainEvent> domainEvents , CancellationToken cancellationToken=default);
    }
}
