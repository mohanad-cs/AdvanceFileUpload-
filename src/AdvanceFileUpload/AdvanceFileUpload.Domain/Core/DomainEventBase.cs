namespace AdvanceFileUpload.Domain.Core
{
    public abstract class DomainEventBase : IDomainEvent
    {
        public Guid Id { get; private set; }
        public DateTime OccurredOn { get; private set; }

        protected DomainEventBase()
        {
            Id = Guid.NewGuid();
            OccurredOn = DateTime.UtcNow;
        }
    }
}
