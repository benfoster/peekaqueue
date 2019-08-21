using System.Collections.Generic;

namespace Peekaqueue
{
    public class MonitoringOptions
    {
        public MonitoringOptions()
        {
            IntervalInSeconds = 10;
            MetricsEndpointPort = 5000;
            MetricsEndpointPath = "metrics/";
            CloudWatchNamespace = "Custom";
            SqsConnectionRetryCount = 10;
            SqsConnectionRetryBackoffMultiplier = 3;
        }

        /// <summary>
        /// The number of retries when it can't connect to sqs
        /// </summary>
        public int SqsConnectionRetryCount { get; set; }

        /// <summary>
        /// A retry backoff multiplier, on every attempt the waiting time is being calculated (multiplier * attempt)  
        /// </summary>
        public int SqsConnectionRetryBackoffMultiplier { get; set; }

        /// <summary>
        /// The delay time in every GetStats iteration    
        /// </summary>
        public int IntervalInSeconds { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<QueueConfiguration> Queues { get; set; }

        /// <summary>
        /// The metric endpoint port that exposes all the metrics related to the queues
        /// </summary>
        public int MetricsEndpointPort { get; set; }

        /// <summary>
        /// The metric relative path that exposes all the metrics related to the queues
        /// </summary>
        public string MetricsEndpointPath { get; set; }

        public string CloudWatchNamespace { get; set; }
    }
}