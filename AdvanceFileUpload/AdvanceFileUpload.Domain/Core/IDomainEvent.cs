using MediatR;

namespace AdvanceFileUpload.Domain.Core
{
    
    //<summary>
    /// Represents a domain event within the system.
    /// </summary>
    public interface IDomainEvent : INotification
    {
        /// <summary>
        /// Gets the unique identifier of the domain event.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets the date and time when the event occurred.
        /// </summary>
        DateTime OccurredOn { get; }
    }
}
