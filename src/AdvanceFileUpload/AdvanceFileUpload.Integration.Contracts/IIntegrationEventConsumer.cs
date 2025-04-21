using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
namespace AdvanceFileUpload.Integration.Contracts
{
    public interface IIntegrationEventConsumer
    {
        void Consume<T>(ConsumingArgs args, Func<T, Task> onMessageReceived) where T : class;
        void Dispose();
    }

    public class ConsumingArgs
    {
        public string Exchange { get; set; }
        public string Queue { get; set; }
        public string RoutingKey { get; set; }
        public string ExchangeType { get; set; }
        public bool Durable { get; set; }
        public bool AutoDelete { get; set; }
        public bool Exclusive { get; set; }
    }
    public class RabbitMQConsumer : IIntegrationEventConsumer
    {
        private readonly RabbitMQOptions _rabbitMQOptions;
        private IConnection? _connection;
        private IChannel? _channel;

        public RabbitMQConsumer(RabbitMQOptions rabbitMQOptions)
        {
            _rabbitMQOptions = rabbitMQOptions;
        }

        private async Task InitializeRabbitMQ()
        {
            var factory = new ConnectionFactory()
            {
                HostName = _rabbitMQOptions.HostName,
                Port = _rabbitMQOptions.Port,
                UserName = _rabbitMQOptions.UserName,
                Password = _rabbitMQOptions.Password,
                VirtualHost = _rabbitMQOptions.VirtualHost,
                Ssl = { Enabled = _rabbitMQOptions.UseSSL }
            };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
        }

        public async void Consume<T>(ConsumingArgs args, Func<T, Task> onMessageReceived) where T : class
        {
            if (_channel is null || _connection is null)
            {
                await InitializeRabbitMQ();
            }

            if (_channel != null)
            {
                await _channel.ExchangeDeclareAsync(exchange: args.Exchange, type: args.ExchangeType, durable: args.Durable, autoDelete: args.AutoDelete);
                await _channel.QueueDeclareAsync(queue: args.Queue, durable: args.Durable, exclusive: args.Exclusive, autoDelete: args.AutoDelete);
                await _channel.QueueBindAsync(queue: args.Queue, exchange: args.Exchange, routingKey: args.RoutingKey);

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
