namespace AdvanceFileUpload.Integration.Contracts
{
    /// <summary>
    /// Defines a contract for consuming integration events.
    /// </summary>
    public interface IIntegrationEventConsumer : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Consumes an integration event of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the event to consume.</typeparam>
        /// <param name="args">The arguments required for consuming the event.</param>
        /// <param name="onMessageReceived">A callback function to handle the received message.</param>
        /// <param name="cancellationToken"></param>
        Task ConsumeAsync<T>(ConsumingArgs args, Func<T, Task> onMessageReceived, CancellationToken cancellationToken = default) where T : class;
    }


}
