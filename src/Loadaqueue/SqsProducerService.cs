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
    public class SqsProducerService : BackgroundService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly ILogger _logger;
        private readonly ProducerOptions _options;

        public SqsProducerService(IAmazonSQS sqsClient, ILogger logger, IOptions<ProducerOptions> options)
        {
            _sqsClient = sqsClient ?? throw new ArgumentNullException(nameof(sqsClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.IsValid())
            {
                _logger.Warning("SQS Producer is not configured or has invalid options");
                return;
            }

            _logger.Information("Producing to queue {QueueUrl}", _options.QueueUrl);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await SendMessagesAsync(stoppingToken);

                    if (_options.SendIntervalInSeconds > 0)
                        await Task.Delay(TimeSpan.FromSeconds(_options.SendIntervalInSeconds), stoppingToken);
                }
            }
            catch (TaskCanceledException) { }
        }



        private async Task SendMessagesAsync(CancellationToken cancellationToken)
        {
            var messages = Enumerable.Range(1, _options.BatchSize)
                            .Select(_ => CreateBatchEntry())
                            .ToList();

            var sendRequest = new SendMessageBatchRequest
            {
                QueueUrl = _options.QueueUrl,
                Entries = messages
            };

            using (_logger.TimeOperation("Send {BatchSize} message(s) to SQS", _options.BatchSize))
            {
                await _sqsClient.SendMessageBatchAsync(sendRequest, cancellationToken);
            }
        }

        private static SendMessageBatchRequestEntry CreateBatchEntry()
        {
            var id = Guid.NewGuid().ToString("n");
            return new SendMessageBatchRequestEntry(id, id);
        }
    }
}
