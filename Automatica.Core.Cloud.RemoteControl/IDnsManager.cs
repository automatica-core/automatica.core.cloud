using System;
using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Cloud.RemoteControl
{
    public interface IDnsManager
    {
        Task<bool> RemoveDnsNameAsync(string name, Guid serverId, Guid? pluginGuid, CancellationToken cancellationToken = default);
        Task<bool> IsDnsNameAvailableAsync(string name, Guid serverId, Guid? pluginGuid, CancellationToken cancellationToken = default);
        Task<(string url, string subDomain)> CreateDnsNameAsync(string name, Guid serverId, Guid? pluginGuid, CancellationToken cancellationToken = default);
    }
}
