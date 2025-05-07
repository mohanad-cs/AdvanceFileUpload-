using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
namespace AdvanceFileUpload.Integration
{
    /// <summary>
    /// Represents a RabbitMQ consumer for handling integration events.
    /// </summary>
    public class RabbitMQConsumer : IIntegrationEventConsumer
    {
        private readonly RabbitMQOptions _rabbitMQOptions;
        private IConnection? _connection;
        private IChannel? _channel;
        private readonly IConnectionFactory _connectionFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitMQConsumer"/> class.
        /// </summary>
        /// <param name="rabbitMQOptions">The configuration options for RabbitMQ.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="rabbitMQOptions"/> is null.</exception>
        public RabbitMQConsumer(RabbitMQOptions rabbitMQOptions)
        {
            _rabbitMQOptions = rabbitMQOptions ?? throw new ArgumentNullException(nameof(rabbitMQOptions));
            _connectionFactory = new ConnectionFactory()
            {
                HostName = _rabbitMQOptions.HostName,
                Port = _rabbitMQOptions.Port,
                UserName = _rabbitMQOptions.UserName,
                Password = _rabbitMQOptions.Password,
                VirtualHost = _rabbitMQOptions.VirtualHost,
                Ssl = { Enabled = _rabbitMQOptions.UseSSL }
            };
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

        /// <summary>
        /// Consumes messages from a RabbitMQ queue and processes them using the provided callback.
        /// </summary>
        /// <typeparam name="T">The type of the message to consume.</typeparam>
        /// <param name="args">The arguments required for consuming the message.</param>
        /// <param name="onMessageReceived">A callback function to handle the received message.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ConsumeAsync<T>(ConsumingArgs args, Func<T, Task> onMessageReceived, CancellationToken cancellationToken = default) where T : class
        {
            await EnsureConnection(cancellationToken);

            if (_channel != null)
            {
                await _channel.ExchangeDeclareAsync(exchange: args.Exchange, type: args.ExchangeType, durable: args.Durable, autoDelete: args.AutoDelete, cancellationToken: cancellationToken);
                await _channel.QueueDeclareAsync(queue: args.Queue, durable: args.Durable, exclusive: args.Exclusive, autoDelete: args.AutoDelete, cancellationToken: cancellationToken);
                await _channel.QueueBindAsync(queue: args.Queue, exchange: args.Exchange, routingKey: args.RoutingKey, cancellationToken: cancellationToken);

                AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var data = JsonSerializer.Deserialize<T>(message);
                    if (data != null)
                    {
                        await onMessageReceived(data);
                    }
                };

                await _channel.BasicConsumeAsync(queue: args.Queue, autoAck: true, consumer: consumer);
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="RabbitMQConsumer"/> and optionally releases the managed resources.
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

        /// <summary>
        /// Asynchronously releases the unmanaged resources used by the <see cref="RabbitMQConsumer"/> and optionally releases the managed resources.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
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
    }

    /// <summary>
    /// Represents the arguments required for consuming messages from a RabbitMQ queue.
    /// </summary>
    public class ConsumingArgs
    {
        /// <summary>
        /// Gets or sets the name of the exchange to consume messages from.
        /// </summary>
        public required string Exchange { get; set; }

        /// <summary>
        /// Gets or sets the name of the queue to consume messages from.
        /// </summary>
        public required string Queue { get; set; }

        /// <summary>
        /// Gets or sets the routing key used for binding the queue to the exchange.
        /// </summary>
        public required string RoutingKey { get; set; }

        /// <summary>
        /// Gets or sets the type of the exchange (e.g., direct, fanout, topic, headers).
        /// </summary>
        public required string ExchangeType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the queue and exchange should be durable.
        /// Durable queues and exchanges survive broker restarts.
        /// </summary>
        public bool Durable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the queue and exchange should be automatically deleted
        /// when no longer in use.
        /// </summary>
        public bool AutoDelete { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the queue should be exclusive to the connection
        /// that declares it.
        /// </summary>
        public bool Exclusive { get; set; }
    }
}
