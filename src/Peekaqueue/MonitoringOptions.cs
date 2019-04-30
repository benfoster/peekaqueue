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
        }

        public int IntervalInSeconds { get; set; }
        public IEnumerable<QueueConfiguration> Queues { get; set; }
        public int MetricsEndpointPort { get; set; }
        public string MetricsEndpointPath { get; set; }
        public string CloudWatchNamespace { get; set; }
    }
}