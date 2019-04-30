using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using MediatR;
using Microsoft.Extensions.Options;
using Serilog;
using SerilogTimings.Extensions;

namespace Peekaqueue
{
    public class CloudWatchMetricsHandler : INotificationHandler<SqsStats>
    {
        private readonly IAmazonCloudWatch _cloudWatchClient;
        private readonly ILogger _logger;
        private readonly MonitoringOptions _options;

        public CloudWatchMetricsHandler(
            ILogger logger,
            IOptions<MonitoringOptions> options,
            IAmazonCloudWatch cloudWatchClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _cloudWatchClient = cloudWatchClient ?? throw new ArgumentNullException(nameof(cloudWatchClient));
        }

        public async Task Handle(SqsStats stats, CancellationToken cancellationToken)
        {
            if (stats == null)
                throw new ArgumentNullException(nameof(stats));

            if (!stats.HasConsumerStats)
                return;

            var metricRequest = new PutMetricDataRequest
            {
                Namespace = _options.CloudWatchNamespace,
                MetricData = new List<MetricDatum>
                {
                    new MetricDatum
                    {
                        MetricName = "EcsServiceBacklog",
                        Value = stats.EcsServiceBacklogCount,
                        Dimensions = {
                            new Dimension { Name = "Queue", Value = stats.QueueName },
                            new Dimension { Name = "Cluster", Value = stats.EcsCluster },
                            new Dimension { Name = "Service", Value = stats.EcsService }
                        }
                    }
                }
            };

            try
            {
                using (_logger.TimeOperation("Sending metrics to CloudWatch"))
                {
                    await _cloudWatchClient.PutMetricDataAsync(metricRequest, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error sending metrics to CloudWatch");
            }
        }
    }
}