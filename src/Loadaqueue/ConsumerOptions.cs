namespace Loadaqueue
{
    public class ConsumerOptions
    {
        public ConsumerOptions()
        {
            BatchSize = 1;
            VisibilityTimeout = 30;
        }

        public string QueueUrl { get; set; }
        public int BatchSize { get; set; }
        public int VisibilityTimeout { get; set; }
        
        /// <summary>
        /// Gets or sets the wait time in seconds used for long polling
        /// </summary>
        public int WaitTimeInSeconds { get; set; }
        public int MessageLatencyInSeconds { get; set; }
        
        /// <summary>
        /// Gets or sets the interval between polling attempts
        /// </summary>
        public int PollingIntervalInSeconds { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(QueueUrl);
        }
    }
}