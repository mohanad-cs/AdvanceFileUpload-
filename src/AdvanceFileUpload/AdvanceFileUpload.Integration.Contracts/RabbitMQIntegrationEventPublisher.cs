using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace AdvanceFileUpload.Integration.Contracts
{
    public class RabbitMQIntegrationEventPublisher : IIntegrationEventPublisher
    {
        private readonly RabbitMQOptions _rabbitMQOptions;
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<RabbitMQIntegrationEventPublisher> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMQIntegrationEventPublisher(IOptions<RabbitMQOptions> rabbitMQOptions, ILogger<RabbitMQIntegrationEventPublisher> logger)
        {
            if (rabbitMQOptions is null)
            {
                throw new ArgumentNullException(nameof(rabbitMQOptions));
            }

            _rabbitMQOptions = rabbitMQOptions.Value;

            _connectionFactory = new ConnectionFactory()
            {
                HostName = _rabbitMQOptions.HostName,
                Port = _rabbitMQOptions.Port,
                UserName = _rabbitMQOptions.UserName,
                Password = _rabbitMQOptions.Password,
                VirtualHost = _rabbitMQOptions.VirtualHost,
                Ssl = { Enabled = _rabbitMQOptions.UseSSL }
            };
            _logger = logger;
        }

        private async Task EnsureConnection(CancellationToken cancellationToken = default)
        {
            if (_connection == null || !_connection.IsOpen)
            {
                _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
            }

            if (_channel == null || !_channel.IsOpen)
            {
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            }
        }

        public async Task PublishAsync<T>(PublishMessage<T> message, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                await EnsureConnection(cancellationToken);

                await _channel.ExchangeDeclareAsync(message.Exchange, message.ExchangeType, message.Durable, message.AutoDelete, null);
                await _channel.QueueDeclareAsync(queue: message.Queue, durable: message.Durable, exclusive: message.Exclusive, autoDelete: message.AutoDelete, arguments: null);
                await _channel.QueueBindAsync(message.Queue, message.Exchange, message.RoutingKey, null);
                var messageSe = JsonSerializer.Serialize<T>(message.Message);
                var body = Encoding.UTF8.GetBytes(messageSe);



                await _channel.BasicPublishAsync(exchange: message.Exchange, routingKey: message.RoutingKey, body: body);
            }
            catch (BrokerUnreachableException ex)
            {
                // Log the exception (use your preferred logging framework)
                _logger.LogError($"RabbitMQ Broker Unreachable: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // Log the exception (use your preferred logging framework)
                _logger.LogError($"Error publishing message: {ex.Message}");
                throw;
            }
            finally
            {
                // Optionally dispose of the channel and connection if not reusing
                _channel?.CloseAsync();
                _connection?.CloseAsync();

            }
        }
    }
}

