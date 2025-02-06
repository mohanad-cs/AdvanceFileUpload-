namespace AdvanceFileUpload.Domain.Core
{
    /// <summary>
    /// Defines a contract for publishing domain events.
    /// </summary>
    public interface IDomainEventPublisher
    {
        /// <summary>
        /// Publishes a single domain event asynchronously.
        /// </summary>
        /// <param name="domainEvent">The domain event to be published.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes multiple domain events asynchronously.
        /// </summary>
        /// <param name="domainEvents">The collection of domain events to be published.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
    }
}
