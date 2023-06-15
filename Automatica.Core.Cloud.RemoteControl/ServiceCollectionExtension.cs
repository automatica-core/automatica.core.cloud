using Azure.ResourceManager.Dns;
using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Cloud.RemoteControl.Configuration;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Dns.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Automatica.Core.Cloud.RemoteControl
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddDnsZoneProvider(this IServiceCollection serviceProvider, Action<DnsManagerConfiguration> configureOptions)
        {
            var optionsBuilder = AddDnsZoneProviderInternal(serviceProvider);
            optionsBuilder.Configure(configureOptions);

            return serviceProvider;
        }
        public static IServiceCollection AddDnsZoneProvider(this IServiceCollection serviceProvider, IConfiguration config)
        {
            AddDnsZoneProviderInternal(serviceProvider);
            serviceProvider.Configure<DnsManagerConfiguration>(config);

            return serviceProvider;
        }

        internal static OptionsBuilder<DnsManagerConfiguration> AddDnsZoneProviderInternal(
            IServiceCollection serviceProvider)
        {
            var optionsBuilder = serviceProvider.AddOptions<DnsManagerConfiguration>();
            serviceProvider.AddSingleton<IDnsManager, DnsManager>();
            serviceProvider.AddSingleton(new ArmClient(new DefaultAzureCredential()));
            return optionsBuilder;
        }
    }
}
