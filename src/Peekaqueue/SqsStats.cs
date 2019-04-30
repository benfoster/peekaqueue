using MediatR;

namespace Peekaqueue
{
    public class SqsStats : INotification
    {
        public SqsStats(
            string queueName,
            string queueUrl,
            int availableMessagesCount,
            int delayedMessagesCount,
            int notVisibleMessagesCount)
        {
            QueueName = queueName;
            QueueUrl = queueUrl;
            AvailableMessagesCount = availableMessagesCount;
            DelayedMessagesCount = delayedMessagesCount;
            NotVisibleMessagesCount = notVisibleMessagesCount;
        }

        public string QueueName { get; }
        public string QueueUrl { get; }
        public int AvailableMessagesCount { get; }
        public int DelayedMessagesCount { get; }
        public int NotVisibleMessagesCount { get; }

        public SqsStats WithConsumerStats(
            string ecsCluster,
            string ecsService,
            int ecsServiceRunningCount,
            int ecsServiceDesiredCount,
            int ecsServicePendingCount)
        {
            HasConsumerStats = true;
            EcsCluster = ecsCluster;
            EcsService = ecsService;
            EcsServiceRunningCount = ecsServiceRunningCount;
            EcsServiceDesiredCount = ecsServiceDesiredCount;
            EcsServicePendingCount = ecsServicePendingCount;   

            return this;
        }

        public bool HasConsumerStats { get; private set; }
        public string EcsCluster { get; private set; }
        public string EcsService { get; private set; }
        public int EcsServiceRunningCount { get; private set; }
        public int EcsServiceDesiredCount { get; private set; }
        public int EcsServicePendingCount { get; private set; }

        public double EcsServiceBacklogCount => (double)AvailableMessagesCount / EcsServiceRunningCount;
    }
}