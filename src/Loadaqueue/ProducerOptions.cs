namespace Loadaqueue
{
    public class ProducerOptions
    {
        public ProducerOptions()
        {
            SendIntervalInSeconds = 1;
        }
        
        public string QueueUrl { get; set; }
        public int BatchSize { get; set; }
        public int SendIntervalInSeconds { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(QueueUrl);    
        }
    }
}