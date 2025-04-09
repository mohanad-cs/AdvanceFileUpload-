
namespace AdvanceFileUpload.Integration.Contracts
{
    public interface IIntegrationEventPublisher
    {
        Task PublishAsync<T>(PublishMessage<T> message, CancellationToken cancellationToken = default) where T : class;
    }
}
