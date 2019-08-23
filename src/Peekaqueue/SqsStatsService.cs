using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.ECS;
using Amazon.ECS.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using SerilogTimings.Extensions;
using Task = System.Threading.Tasks.Task;
using Polly;

namespace Peekaqueue
{
    public class SqsStatsService : BackgroundService
    {
        private static List<string> QueueAttributes = new List<string>
        {
            QueueAttributeName.ApproximateNumberOfMessages,
            QueueAttributeName.ApproximateNumberOfMessagesDelayed,
            QueueAttributeName.ApproximateNumberOfMessagesNotVisible
        };

        private readonly ILogger _logger;
        private readonly IAmazonSQS _sqsClient;
        private readonly IAmazonECS _ecsClient;
        private readonly MonitoringOptions _monitoringOptions;
        private readonly IMediator _mediator;
        private readonly Policy _retryPolicy;
        private readonly IApplicationLifetime _applicationLifetime;

        public SqsStatsService(
            ILogger logger,
            IOptions<MonitoringOptions> monitoringOptions,
            IAmazonSQS sqsClient,
            IAmazonECS ecsClient,
            IMediator mediator, 
            IApplicationLifetime applicationLifetime)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _monitoringOptions = monitoringOptions.Value ?? throw new ArgumentNullException(nameof(monitoringOptions));
            _sqsClient = sqsClient ?? throw new ArgumentNullException(nameof(sqsClient));
            _ecsClient = ecsClient ?? throw new ArgumentNullException(nameof(ecsClient));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));

            _retryPolicy = Policy.Handle<Exception>()
               .WaitAndRetryAsync(_monitoringOptions.SqsConnectionRetryCount,
                attempts => TimeSpan.FromSeconds(attempts * _monitoringOptions.SqsConnectionRetryBackoffMultiplier), OnRetryAsync);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var queues = await GetQueuesToMonitor();

                if (queues.Count > 0)
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        await GetStatsAsync(queues, stoppingToken);
                        await Task.Delay(TimeSpan.FromSeconds(_monitoringOptions.IntervalInSeconds), stoppingToken);
                    }
                }
                else
                {
                    _logger.Warning("No queues are available for monitoring");
                    _logger.Warning("Application is terminating");
                    _applicationLifetime.StopApplication();
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.Error(ex, ex.Message);
            }
        }

        private async Task<Dictionary<QueueConfiguration, string>> GetQueuesToMonitor()
        {
            var queues = new Dictionary<QueueConfiguration, string>();

            foreach (var queueConfiguration in _monitoringOptions.Queues)
            {
                using (_logger.TimeOperation("Getting SQS queue URL for {QueueName}", queueConfiguration.Name))
                {
                    try
                    {
                        GetQueueUrlResponse queueUrlResponse = await _retryPolicy.ExecuteAsync(_ =>
                            _sqsClient.GetQueueUrlAsync(queueConfiguration.Name), CreateRetryPolicyContext(queueConfiguration.Name));

                        if (queueUrlResponse.HttpStatusCode == HttpStatusCode.OK)
                        {
                            queues.Add(queueConfiguration, queueUrlResponse.QueueUrl);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Unable to get queue URL for {QueueName}", queueConfiguration.Name);
                    }
                }
            }

            return queues;
        }

        private async Task GetStatsAsync(Dictionary<QueueConfiguration, string> queues, CancellationToken stoppingToken)
        {
            var statsTasks = queues.Select(q => GetQueueStatsAsync(q.Key, q.Value, stoppingToken));
            await Task.WhenAll(statsTasks);
        }

        private async Task GetQueueStatsAsync(QueueConfiguration queueConfiguration, string url, CancellationToken stoppingToken)
        {
            GetQueueAttributesResponse queueAttributes = await GetQueueAttributesAsync(queueConfiguration.Name, url, stoppingToken);

            if (queueAttributes?.HttpStatusCode != HttpStatusCode.OK)
                return;

            var stats = new SqsStats(
                queueConfiguration.Name,
                url,
                queueAttributes.ApproximateNumberOfMessages,
                queueAttributes.ApproximateNumberOfMessagesDelayed,
                queueAttributes.ApproximateNumberOfMessagesNotVisible
            );

            if (queueConfiguration.HasConsumer)
            {
                DescribeServicesResponse serviceResponse = await GetServiceDetailsAsync(queueConfiguration.ConsumerCluster, queueConfiguration.ConsumerService);

                if (serviceResponse?.HttpStatusCode == HttpStatusCode.OK && serviceResponse.Services.Any())
                {
                    Service serviceDetails = serviceResponse.Services.First();

                    stats = stats.WithConsumerStats(
                        queueConfiguration.ConsumerCluster,
                        queueConfiguration.ConsumerService,
                        serviceDetails.RunningCount,
                        serviceDetails.DesiredCount,
                        serviceDetails.PendingCount
                    );
                }
            }

            await _mediator.Publish(stats, stoppingToken);
        }

        private async Task<GetQueueAttributesResponse> GetQueueAttributesAsync(string queueName, string queueUrl, CancellationToken stoppingToken)
        {
            try
            {
                using (_logger.TimeOperation("Getting SQS queue attributes for {QueueName}", queueName))
                {
                    return await _retryPolicy.ExecuteAsync(_ => _sqsClient.GetQueueAttributesAsync(queueUrl, QueueAttributes, stoppingToken), CreateRetryPolicyContext(queueName));
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error getting queue attributes for {QueueName}", queueName);
                return null;
            }
        }

        private async Task<DescribeServicesResponse> GetServiceDetailsAsync(string cluster, string service)
        {
            try
            {
                using (_logger.TimeOperation("Getting ECS service details for {Cluster}:{Service}", cluster, service))
                {
                    return await _ecsClient.DescribeServicesAsync(new DescribeServicesRequest
                    {
                        Cluster = cluster,
                        Services = { service }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Erorr getting ECS service details for {Cluster}:{Service}", cluster, service);
                return null;
            }
        }

        private Task OnRetryAsync(Exception ex, TimeSpan delay, int attempts, Context context)
        {
            _logger.Warning("Failed to connect to SQS queue {Queue}. Attempt {Attempt}/{Max}", context["Queue"], attempts, _monitoringOptions.SqsConnectionRetryCount);
            return Task.CompletedTask;
        }

        private static Context CreateRetryPolicyContext(string queueName)
        {
            var policyContext = new Context();
            policyContext.Add("Queue", queueName);
            return policyContext;
        }
    }
}