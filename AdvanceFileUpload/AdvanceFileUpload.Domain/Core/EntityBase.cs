using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Domain.Core
{
    public abstract class EntityBase
    {
        public Guid Id { get; protected set; }
        private readonly List<IDomainEvent> _domainEvents = [];

        protected EntityBase()
        {
            Id = Guid.NewGuid();
        }
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
        public override bool Equals(object? obj)
        {
            if (obj == null || obj.GetType() != this.GetType())
            {
                return false;
            }

            var other = (EntityBase)obj;
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static bool operator ==(EntityBase left, EntityBase right)
        {
            if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
                return true;

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;

            return left.Equals(right);
        }

        public static bool operator !=(EntityBase left, EntityBase right)
        {
            return !(left == right);
        }
    }
}
