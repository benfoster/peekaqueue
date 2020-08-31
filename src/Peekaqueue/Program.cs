using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.ECS;
using Amazon.SQS;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Extensions.Logging;

namespace Peekaqueue
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new HostBuilder()
                .ConfigureLogging((context, loggerFactory) =>
                {
                    var logger = ConfigureLogging(context.Configuration);
                    loggerFactory.AddSerilog(logger);
                })
                .ConfigureAppConfiguration(ConfigureConfiguration)
                .ConfigureServices(ConfigureServices)
                .RunConsoleAsync();
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddMediatR();
            services.AddSingleton(Log.Logger);
            services.AddDefaultAWSOptions(context.Configuration.GetAWSOptions());
            services.AddAWSServiceWithOverride<IAmazonSQS>(context.Configuration, "AWS:Sqs");
            services.AddAWSService<IAmazonECS>();
            services.AddAWSService<IAmazonCloudWatch>();

            services.AddHostedService<SqsStatsService>();
            services.AddHostedService<MetricsEndpointService>();
            services.Configure<MonitoringOptions>(context.Configuration.GetSection("Monitoring"));
        }

        private static void ConfigureConfiguration(HostBuilderContext hostingContext, IConfigurationBuilder configuration)
        {
            configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.local.json", optional: true)
                .AddEnvironmentVariables(prefix: "PEEKAQ_");
        }

        private static ILogger ConfigureLogging(IConfiguration configuration)
        {
            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            Log.Logger = logger;
            return logger;
        }
    }
}
