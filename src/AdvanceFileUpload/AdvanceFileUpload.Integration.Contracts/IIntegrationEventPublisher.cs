using MassTransit;

namespace AdvanceFileUpload.Integration.Contracts
{
    public interface IIntegrationEventPublisher
    {
        Task PublishAsync<TIntegrationEvent>(TIntegrationEvent @event , CancellationToken cancellationToken=default)
            where TIntegrationEvent : class;
    }

    public class IntegrationEventPublisher : IIntegrationEventPublisher
    {
        private readonly IPublishEndpoint _publishEndpoint;
        public IntegrationEventPublisher(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }
        public async Task PublishAsync<TIntegrationEvent>(TIntegrationEvent @event , CancellationToken cancellationToken =default)
            where TIntegrationEvent : class
        {
            await _publishEndpoint.Publish(@event , cancellationToken);
        }
    }
}
