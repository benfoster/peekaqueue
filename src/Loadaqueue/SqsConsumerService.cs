using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using SerilogTimings.Extensions;

namespace Loadaqueue
{
    public class SqsConsumerService : BackgroundService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly ILogger _logger;
        private readonly ConsumerOptions _options;

        public SqsConsumerService(IAmazonSQS sqsClient, ILogger logger, IOptions<ConsumerOptions> options)
        {
            _sqsClient = sqsClient ?? throw new ArgumentNullException(nameof(sqsClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.IsValid())
            {
                _logger.Warning("SQS Consumer is not configured or has invalid options");
                return;
            }

            _logger.Information("Consuming queue {QueueUrl}", _options.QueueUrl);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await PollQueueAsync(stoppingToken);

                    if (_options.PollingIntervalInSeconds > 0)
                        await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalInSeconds), stoppingToken);
                }
            }
            catch (TaskCanceledException) {}
        }

        private async Task PollQueueAsync(CancellationToken cancellationToken)
        {
            var receiveRequest = new ReceiveMessageRequest
            {
                QueueUrl = _options.QueueUrl,
                MaxNumberOfMessages = _options.BatchSize,
                VisibilityTimeout = _options.VisibilityTimeout,
                WaitTimeSeconds = _options.WaitTimeInSeconds
            };

            ReceiveMessageResponse receiveResponse;

            using (var op = _logger.ForContext("QueueUrl", _options.QueueUrl)
                .BeginOperation("Receiving messages from SQS"))
            {
                receiveResponse = await _sqsClient.ReceiveMessageAsync(receiveRequest, cancellationToken);
                op.Complete();
            }

            var processTasks = receiveResponse.Messages.Select(
                message => ProcessMessageAsync(message, cancellationToken)
            );

            await Task.WhenAll(processTasks);
        }

        private async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
        {
            using (_logger.TimeOperation("Processing message {MessageId}", message.MessageId))
            {
                if (_options.MessageLatencyInSeconds > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.MessageLatencyInSeconds), cancellationToken);
                }
            }

            using (_logger.TimeOperation("Deleting messsage {MessageId}", message.MessageId))
            {
                await _sqsClient.DeleteMessageAsync(_options.QueueUrl, message.ReceiptHandle, cancellationToken);
            }
        }
    }
}

