namespace AdvanceFileUpload.Integration.Contracts
{
    public class PublishMessage<T>
    {
        public T Message { get; set; }
        public string Queue { get; set; } // Corrected typo from Quque to Queue
        public string Exchange { get; set; }
        public string ExchangeType { get; set; }
        public string RoutingKey { get; set; }
        public bool Durable { get; set; }
        public bool Exclusive { get; set; }
        public bool AutoDelete { get; set; }
    }
}
