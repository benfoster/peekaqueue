using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Prometheus;
using Serilog;

namespace Peekaqueue
{
    public class MetricsEndpointService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly MonitoringOptions _options;
        private readonly IMetricServer _server;

        public MetricsEndpointService(ILogger logger, IOptions<MonitoringOptions> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _server = new MetricServer(_options.MetricsEndpointPort, _options.MetricsEndpointPath);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.Debug($"Starting metrics server on :{_options.MetricsEndpointPort} {_options.MetricsEndpointPath}");

            try
            {
                _server.Start();
            }
            catch (Exception ex)
            {
                _logger.Error("Error starting metrics server", ex);
            }

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Debug("Stopping metrics server");

            try
            {
                await _server.StopAsync();
            }
            catch (Exception ex)
            {
                _logger.Error("Error stopping metrics server", ex);
            }
        }

        public void Dispose()
        {
            _server?.Dispose();
        }
    }
}