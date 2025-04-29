namespace AdvanceFileUpload.Integration.Contracts
{
    /// <summary>
    /// Represents a message to be published to a message broker.
    /// </summary>
    /// <typeparam name="T">The type of the message payload.</typeparam>
    public class PublishMessage<T>
    {
        /// <summary>
        /// Gets or sets the message payload.
        /// </summary>
        public required T Message { get; set; }

        /// <summary>
        /// Gets or sets the name of the queue to which the message will be published.
        /// </summary>
        public required string Queue { get; set; }

        /// <summary>
        /// Gets or sets the name of the exchange to which the message will be published.
        /// </summary>
        public required string Exchange { get; set; }

        /// <summary>
        /// Gets or sets the type of the exchange (e.g., direct, fanout, topic, headers).
        /// </summary>
        public required string ExchangeType { get; set; }

        /// <summary>
        /// Gets or sets the routing key used to route the message.
        /// </summary>
        public required string RoutingKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the queue is durable.
        /// Durable queues survive broker restarts.
        /// </summary>
        public bool Durable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the queue is exclusive.
        /// Exclusive queues can only be accessed by the connection that declared them.
        /// </summary>
        public bool Exclusive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the queue is automatically deleted
        /// when the last consumer unsubscribes.
        /// </summary>
        public bool AutoDelete { get; set; }
    }
}
