namespace AdvanceFileUpload.Integration.Contracts
{
    public class RabbitMQOptions
    {
        public const string SectionName = "RabbitMQOptions";
        public string HostName { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; }
        public bool UseSSL { get; set; }
    }
}
