using Automatica.Core.Cloud.RemoteControl.Configuration;
using Azure.Identity;
using Azure.ResourceManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Automatica.Core.Cloud.RemoteControl
{
    public static class ServiceCollectionExtension
    {
        /// <summary>
        /// Registers the Azure DNS based sub domain manager. Authenticates with DefaultAzureCredential, i.e. the
        /// App Service managed identity in Azure and the developer login locally. No client secret is used.
        /// </summary>
        public static IServiceCollection AddDnsZoneProvider(this IServiceCollection services, IConfigurationSection config)
        {
            services.AddOptions<DnsManagerConfiguration>().Bind(config);
            services.AddSingleton<IDnsManager, DnsManager>();
            services.AddSingleton(_ => new ArmClient(new DefaultAzureCredential()));
            return services;
        }
    }
}
