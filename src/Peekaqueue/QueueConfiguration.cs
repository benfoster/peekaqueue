namespace Peekaqueue
{
    public class QueueConfiguration
    {
        public string Name { get; set; }
        public string ConsumerCluster { get; set; }
        public string ConsumerService { get; set; }

        public bool HasConsumer => !string.IsNullOrWhiteSpace(ConsumerCluster) && !string.IsNullOrWhiteSpace(ConsumerService);
    }
}