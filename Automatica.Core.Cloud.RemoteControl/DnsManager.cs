using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Cloud.RemoteControl.Configuration;
using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Dns;
using Azure.ResourceManager.Resources;
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

        private async Task<DnsZoneResource> GetDnsZoneResource(CancellationToken cancellationToken)
        {
            var config = _config.CurrentValue;
            SubscriptionResource subscription;
            if (!string.IsNullOrEmpty(config.SubscriptionId))
            {
                subscription = _armClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{config.SubscriptionId}"));
            }
            else
            {
                subscription = await _armClient.GetDefaultSubscriptionAsync(cancellationToken);
            }

            ResourceGroupResource resourceGroup = await subscription.GetResourceGroups().GetAsync(config.ResourceGroup, cancellationToken);
            // the zone is managed by terraform; never create or update it from here
            DnsZoneResource zone = await resourceGroup.GetDnsZones().GetAsync(config.DnsZoneName, cancellationToken);
            return zone;
        }

        /// <summary>The record name inside the zone, e.g. "foo" -> "foo.dev".</summary>
        private string RecordName(string name)
        {
            var prefix = _config.CurrentValue.CNamePrefix;
            return string.IsNullOrEmpty(prefix) ? name : $"{name}.{prefix}";
        }

        private static bool BelongsTo(DnsCnameRecordResource record, Guid serverId, Guid? pluginGuid)
        {
            record.Data.Metadata.TryGetValue("ServerId", out var serverIdString);
            if (serverIdString == null || !Guid.TryParse(serverIdString, out var owner) || owner != serverId)
            {
                return false;
            }

            if (pluginGuid.HasValue)
            {
                record.Data.Metadata.TryGetValue("PluginGuid", out var pluginGuidString);
                if (pluginGuidString == null || !Guid.TryParse(pluginGuidString, out var plugin) || plugin != pluginGuid.Value)
                {
                    return false;
                }
            }
            return true;
        }

        private static void EnsureValid(string name)
        {
            if (!SubDomainName.IsValid(name))
            {
                throw new ArgumentException("Invalid sub domain name");
            }
        }

        public async Task<bool> RemoveDnsNameAsync(string name, Guid serverId, Guid? pluginGuid, CancellationToken cancellationToken = default)
        {
            name = SubDomainName.Normalize(name);
            if (!SubDomainName.IsValid(name))
            {
                return false;
            }
            _logger.LogInformation("Remove dns name {Name}", name);

            var dnsZone = await GetDnsZoneResource(cancellationToken);
            try
            {
                var cnameRecord = await dnsZone.GetDnsCnameRecords().GetAsync(RecordName(name), cancellationToken);

                if (cnameRecord.HasValue)
                {
                    if (!BelongsTo(cnameRecord.Value, serverId, pluginGuid))
                    {
                        return false;
                    }

                    await cnameRecord.Value.DeleteAsync(WaitUntil.Completed, null, cancellationToken);
                    return true;
                }
            }
            catch (RequestFailedException rfe) when (rfe.Status == (int)HttpStatusCode.NotFound)
            {
                return true;
            }

            return false;
        }

        public async Task<bool> IsDnsNameAvailableAsync(string name, Guid serverId, Guid? pluginGuid, CancellationToken cancellationToken = default)
        {
            name = SubDomainName.Normalize(name);
            if (!SubDomainName.IsValid(name))
            {
                return false;
            }
            _logger.LogInformation("Checking if dns name {Name} is available", name);

            var dnsZone = await GetDnsZoneResource(cancellationToken);
            try
            {
                var cnameRecord = await dnsZone.GetDnsCnameRecords().GetAsync(RecordName(name), cancellationToken);
                if (cnameRecord.HasValue)
                {
                    return BelongsTo(cnameRecord.Value, serverId, pluginGuid);
                }
            }
            catch (RequestFailedException rfe) when (rfe.Status == (int)HttpStatusCode.NotFound)
            {
                return true;
            }

            return false;
        }

        public async Task<(string url, string subDomain)> CreateDnsNameAsync(string name, Guid serverId, Guid? pluginGuid, CancellationToken cancellationToken = default)
        {
            name = SubDomainName.Normalize(name);
            EnsureValid(name);

            var isAvailable = await IsDnsNameAvailableAsync(name, serverId, pluginGuid, cancellationToken);
            if (!isAvailable)
            {
                throw new ArgumentException($"DnsName {name} already taken!");
            }

            var config = _config.CurrentValue;
            var dnsZone = await GetDnsZoneResource(cancellationToken);
            var all = dnsZone.GetDnsCnameRecords();

            var dnsCnameRecord = new DnsCnameRecordData
            {
                Cname = config.CNameTarget,
                TtlInSeconds = 3600,
                Metadata =
                {
                    {"ServerId", $"{serverId}"},
                    {"PluginGuid", $"{pluginGuid ?? serverId}"}
                }
            };

            var recordName = RecordName(name);
            await all.CreateOrUpdateAsync(WaitUntil.Completed, recordName, dnsCnameRecord, cancellationToken: cancellationToken);
            return ($"{recordName}.{config.DnsZoneName}", recordName);
        }
    }
}
