using System.IO;
using System.Threading.Tasks;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Loadaqueue
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
            services.AddSingleton(Log.Logger);
            services.AddDefaultAWSOptions(context.Configuration.GetAWSOptions());
            services.AddAWSService<IAmazonSQS>();

            services.Configure<ProducerOptions>(context.Configuration.GetSection("Producer"));
            services.Configure<ConsumerOptions>(context.Configuration.GetSection("Consumer"));

            services.AddHostedService<SqsConsumerService>();
            services.AddHostedService<SqsProducerService>();
        }

        private static void ConfigureConfiguration(HostBuilderContext hostingContext, IConfigurationBuilder configuration)
        {
            configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.local.json", optional: true)
                .AddEnvironmentVariables(prefix: "LOADAQ_");
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