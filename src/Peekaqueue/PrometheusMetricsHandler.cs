using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Prometheus;

namespace Peekaqueue
{
    public class PrometheusMetricsHandler : INotificationHandler<SqsStats>
    {
        private static readonly Gauge AvailableMessagesGauge 
            = Metrics.CreateGauge("sqs_available_messages_count", "The number of messages available for retrieval from the queue", Labels.Queue);

        private static readonly Gauge DelayedMessagesGauge 
            = Metrics.CreateGauge("sqs_delayed_messages_count", "The number of messages in the queue that are delayed and not available for reading immediately", Labels.Queue);

        private static readonly Gauge NotVisibleMessagesGauge 
            = Metrics.CreateGauge("sqs_not_visible_messages_count", "The number of messages that are in flight", Labels.Queue);

        private static readonly Gauge EcsServiceRunningCountGauge
            = Metrics.CreateGauge("ecs_service_running_count", "The number of tasks in the cluster that are in the RUNNING state", Labels.EcsCluster, Labels.EcsService);

        private static readonly Gauge EcsServiceDesiredCountGauge
            = Metrics.CreateGauge("ecs_service_desired_count", "The desired number of instantiations of the task definition to keep running on the service", Labels.EcsCluster, Labels.EcsService);

        private static readonly Gauge EcsServicePendingCountGauge
            = Metrics.CreateGauge("ecs_service_pending_count", "The number of tasks in the cluster that are in the PENDING state", Labels.EcsCluster, Labels.EcsService);

        private static readonly Gauge EcsServiceBacklogCountGauge
            = Metrics.CreateGauge("ecs_service_backlog_count", "The SQS messages backlog per task", "queue", Labels.EcsCluster, Labels.EcsService);


        public Task Handle(SqsStats stats, CancellationToken cancellationToken)
        {
            if (stats == null)
                throw new ArgumentNullException(nameof(stats));

            AvailableMessagesGauge.WithLabels(stats.QueueName).Set(stats.AvailableMessagesCount);
            DelayedMessagesGauge.WithLabels(stats.QueueName).Set(stats.DelayedMessagesCount);
            NotVisibleMessagesGauge.WithLabels(stats.QueueName).Set(stats.NotVisibleMessagesCount);

            if (stats.HasConsumerStats)
            {
                EcsServiceRunningCountGauge.WithLabels(stats.EcsCluster, stats.EcsService).Set(stats.EcsServiceRunningCount);
                EcsServiceDesiredCountGauge.WithLabels(stats.EcsCluster, stats.EcsService).Set(stats.EcsServiceDesiredCount);
                EcsServicePendingCountGauge.WithLabels(stats.EcsCluster, stats.EcsService).Set(stats.EcsServicePendingCount);
                EcsServiceBacklogCountGauge.WithLabels(stats.QueueName, stats.EcsCluster, stats.EcsService).Set(stats.EcsServiceBacklogCount);
            }

            return Task.CompletedTask;
        }

        private class Labels
        {
            public const string Queue = "queue";
            public const string EcsCluster = "ecs_cluster";
            public const string EcsService = "ecs_service";
        }
    }
}