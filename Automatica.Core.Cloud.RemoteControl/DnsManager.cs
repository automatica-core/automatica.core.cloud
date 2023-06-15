using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Cloud.RemoteControl.Configuration;
using Azure.Identity;
using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Dns;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Automatica.Core.Cloud.RemoteControl
{
    internal class DnsManager : IDnsManager
    {
        private readonly ArmClient _armClient;
        private readonly IOptionsMonitor<DnsManagerConfiguration> _config;
        private readonly ILogger _logger;

        public DnsManager(ArmClient armClient, IOptionsMonitor<DnsManagerConfiguration> config, ILogger<DnsManager> logger)
        {
            _armClient = armClient;
            _config = config;
            _logger = logger;
        }

        private async Task<DnsZoneResource> GetDnsZoneResource()
        {
            var subscription = await _armClient.GetDefaultSubscriptionAsync();
            
            ResourceGroupResource resourceGroup = await subscription.GetResourceGroups().GetAsync(_config.CurrentValue.ResourceGroup);
            var dnsZoneCollection = resourceGroup.GetDnsZones();

            var data1 = new DnsZoneData("Global");
            var dnsZoneLro = await dnsZoneCollection.CreateOrUpdateAsync(WaitUntil.Completed, _config.CurrentValue.DnsZoneName, data1);
            return dnsZoneLro.Value;
        }


        public async Task<bool> IsDnsNameAvailableAsync(string name, Guid serverId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Checking if dns name {name} is available...");

            var dnsZone = await GetDnsZoneResource();
            try
            {
                var cnameRecord = await dnsZone.GetDnsCnameRecords().GetAsync(name, cancellationToken);

                if (cnameRecord.HasValue)
                {
                    cnameRecord.Value.Data.Metadata.TryGetValue("ServerId", out var serverIdString);

                    if (serverIdString == null || serverId != Guid.Parse(serverIdString))
                    {
                        return false;
                    }

                    return true;
                }
            }
            catch (RequestFailedException rfe)
            {
                if (rfe.Status == (int)HttpStatusCode.NotFound)
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<string> CreateDnsNameAsync(string name, Guid serverId, CancellationToken cancellationToken = default)
        {
            var isAvailable = await IsDnsNameAvailableAsync(name, serverId, cancellationToken);

            if (!isAvailable)
            {
                throw new ArgumentException($"DnsName {name} already taken!");
            }
            var dnsZone = await GetDnsZoneResource();
            var all = dnsZone.GetDnsCnameRecords();

            var dnsCnameRecord = new DnsCnameRecordData()
            {
                Cname = _config.CurrentValue.CNameTarget,
                TtlInSeconds = 3600,
                Metadata = { {"ServerId", $"{serverId}"} }
            };

            if (!String.IsNullOrEmpty(_config.CurrentValue.CNamePrefix))
            {
                name = $"{name}.{_config.CurrentValue.CNamePrefix}";
            }

            await all.CreateOrUpdateAsync(WaitUntil.Completed, name, dnsCnameRecord, cancellationToken: cancellationToken);
            return $"{name}.{_config.CurrentValue.DnsZoneName}";
        }
    }
}
