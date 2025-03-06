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
    /// <summary>
    ///  Represents a domain event publisher.
    /// </summary>
    public class DomainEventPublisher : IDomainEventPublisher
    {
        private readonly IPublisher _publisher;
        private readonly ILogger<DomainEventPublisher> _logger;
        /// <summary>
        /// Initializes a new instance of the <see cref="DomainEventPublisher"/> class.
        /// </summary>
        /// <param name="publisher"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
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
            _logger.LogInformation($"Domain event [{domainEvent.GetType().Name}] with id {domainEvent.Id} is Published ");

        }
        ///<inheritdoc/>
        public async Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            foreach (var domainEvent in domainEvents)
            {
                _logger.LogInformation($"Publishing domain event [{domainEvent.GetType().Name}] with id {domainEvent.Id} ");
                await _publisher.Publish(domainEvent, cancellationToken);
                _logger.LogInformation($"Domain event [{domainEvent.GetType().Name}] with id {domainEvent.Id} is Published ");
            }
        }


    }
}
