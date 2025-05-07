namespace AdvanceFileUpload.Integration
{
    /// <summary>
    /// Represents the configuration options for connecting to a RabbitMQ server.
    /// </summary>
    public class RabbitMQOptions
    {
        /// <summary>
        /// The name of the configuration section for RabbitMQ options.
        /// </summary>
        public const string SectionName = "RabbitMQOptions";

        /// <summary>
        /// Gets or sets the hostname of the RabbitMQ server.
        /// </summary>
        public required string HostName { get; set; }

        /// <summary>
        /// Gets or sets the port number used to connect to the RabbitMQ server.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the username for authenticating with the RabbitMQ server.
        /// </summary>
        public required string UserName { get; set; }

        /// <summary>
        /// Gets or sets the password for authenticating with the RabbitMQ server.
        /// </summary>
        public required string Password { get; set; }

        /// <summary>
        /// Gets or sets the virtual host to use when connecting to the RabbitMQ server.
        /// </summary>
        public required string VirtualHost { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether SSL should be used for the connection.
        /// </summary>
        public bool UseSSL { get; set; }
    }
}
