using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdvanceFileUpload.Domain.Core;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AdvanceFileUpload.Application.EventHandling
{
    public class DomainEventPublisher : IDomainEventPublisher
    {
       private readonly IPublisher _publisher;
        private readonly ILogger<DomainEventPublisher> _logger;
        public DomainEventPublisher(IPublisher publisher, ILogger<DomainEventPublisher> logger)
        {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        ///<inheritdoc/>
        public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Publishing domain event [{domainEvent.GetType().Name}] with id {domainEvent.Id} ");
             await _publisher.Publish(domainEvent, cancellationToken);
        }
        ///<inheritdoc/>
        public async Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            foreach (var domainEvent in domainEvents)
            {
                _logger.LogInformation($"Publishing domain event [{domainEvent.GetType().Name}] with id {domainEvent.Id} ");
                await _publisher.Publish(domainEvent, cancellationToken);
            }
        }
    }
}
