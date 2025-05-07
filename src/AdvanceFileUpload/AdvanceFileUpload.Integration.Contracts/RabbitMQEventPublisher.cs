using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace AdvanceFileUpload.Integration
{
    /// <summary>
    /// A publisher for RabbitMQ integration events, implementing the <see cref="IIntegrationEventPublisher"/> interface.
    /// </summary>
    public class RabbitMQEventPublisher : IIntegrationEventPublisher
    {
        private readonly RabbitMQOptions _rabbitMQOptions;
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<RabbitMQEventPublisher> _logger;
        private IConnection? _connection;
        private IChannel? _channel;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitMQEventPublisher"/> class.
        /// </summary>
        /// <param name="rabbitMQOptions">The RabbitMQ configuration options.</param>
        /// <param name="logger">The logger instance for logging events and errors.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="rabbitMQOptions"/> is null.</exception>
        public RabbitMQEventPublisher(IOptions<RabbitMQOptions> rabbitMQOptions, ILogger<RabbitMQEventPublisher> logger)
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

        /// <summary>
        /// Ensures that a connection and channel to RabbitMQ are established.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
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

        ///<inheritdoc/>
        /// <exception cref="BrokerUnreachableException">Thrown when the RabbitMQ broker is unreachable.</exception>
        /// <exception cref="Exception">Thrown when an error occurs during message publishing.</exception>
        public async Task PublishAsync<T>(PublishMessage<T> message, CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                await EnsureConnection(cancellationToken);
                if (_channel != null)
                {
                    await _channel.ExchangeDeclareAsync(message.Exchange, message.ExchangeType, message.Durable, message.AutoDelete, null, cancellationToken: cancellationToken);
                    await _channel.QueueDeclareAsync(queue: message.Queue, durable: message.Durable, exclusive: message.Exclusive, autoDelete: message.AutoDelete, arguments: null, cancellationToken: cancellationToken);
                    await _channel.QueueBindAsync(message.Queue, message.Exchange, message.RoutingKey, null, cancellationToken: cancellationToken);
                    var messageSe = JsonSerializer.Serialize<T>(message.Message);
                    var body = Encoding.UTF8.GetBytes(messageSe);
                    await _channel.BasicPublishAsync(exchange: message.Exchange, routingKey: message.RoutingKey, body: body, cancellationToken: cancellationToken);
                }
            }
            catch (BrokerUnreachableException ex)
            {
                _logger.LogError($"RabbitMQ Broker Unreachable: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error publishing message: {ex.Message}");
                throw;
            }
            finally
            {
                await DisposeAsync();
            }
        }

        /// <summary>
        /// Asynchronously disposes the resources used by the <see cref="RabbitMQEventPublisher"/>.
        /// </summary>
        /// <returns>A ValueTask that represents the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_channel != null)
            {
                await _channel.DisposeAsync();
            }

            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }

        }

        /// <summary>
        /// Disposes the resources used by the  <see cref="RabbitMQEventPublisher"/>.
        /// </summary>
        public void Dispose()
        {
            if (_channel != null)
            {
                _channel.Dispose();
            }

            if (_connection != null)
            {
                _connection.Dispose();
            }
        }
    }
}

