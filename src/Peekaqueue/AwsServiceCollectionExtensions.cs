using System;
using Amazon.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Peekaqueue
{
    public static class AwsServiceCollectionExtensions
    {
        public static IServiceCollection AddAWSServiceWithOverride<T>(
            this IServiceCollection services,
            IConfiguration configuration,
            string overrideSectionName,
            ServiceLifetime lifetime = ServiceLifetime.Singleton) where T : IAmazonService
        {
            if (services == null) throw new ArgumentException(nameof(services));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            return !string.IsNullOrWhiteSpace(overrideSectionName) && configuration.GetSection(overrideSectionName).Exists() ? 
                services.AddAWSService<T>(configuration.GetAWSOptions(overrideSectionName), lifetime) : services.AddAWSService<T>(lifetime);
        }
    }
}