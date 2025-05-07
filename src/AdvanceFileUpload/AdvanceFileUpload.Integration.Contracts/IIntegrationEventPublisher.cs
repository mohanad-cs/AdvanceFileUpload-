
namespace AdvanceFileUpload.Integration
{
    /// <summary>
    /// Defines a contract for publishing integration events to a message broker or event bus.
    /// </summary>
    public interface IIntegrationEventPublisher : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Publishes an integration event message asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of the message payload.</typeparam>
        /// <param name="message">The message to be published, including metadata such as queue, exchange, and routing details.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous publish operation.</returns>
        Task PublishAsync<T>(PublishMessage<T> message, CancellationToken cancellationToken = default) where T : class;
    }
}
