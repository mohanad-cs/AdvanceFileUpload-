namespace AdvanceFileUpload.Domain.Core
{
    /// <summary>
    /// Represents the base class for all domain events.
    /// </summary>
    public abstract class DomainEventBase : IDomainEvent
    {
        ///<inheritdoc/>
        public Guid Id { get; private set; }

        ///<inheritdoc/>
        public DateTime OccurredOn { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainEventBase"/> class.
        /// </summary>
        protected DomainEventBase()
        {
            Id = Guid.NewGuid();
            OccurredOn = DateTime.UtcNow;
        }
    }
}
