using MassTransit;

namespace AdvanceFileUpload.Integration.Contracts
{
    /// <summary>
    /// Defines a consumer interface for handling messages of type <typeparamref name="TMessage"/>.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    public interface IIntegrationEventConsumer<in TMessage> : IConsumer where TMessage : class
    {
        /// <summary>
        /// Consumes the specified message.
        /// </summary>
        /// <param name="context">The context of the message being consumed.</param>
        /// <returns>A task that represents the asynchronous consume operation.</returns>
        Task Consume(ConsumeContext<TMessage> context);
    }
}
