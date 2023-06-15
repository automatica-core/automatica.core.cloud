using System;
using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Cloud.RemoteControl
{
    public interface IDnsManager
    {
        Task<bool> IsDnsNameAvailableAsync(string name, Guid serverId, CancellationToken cancellationToken = default);
        Task<string> CreateDnsNameAsync(string name, Guid serverId, CancellationToken cancellationToken = default);
    }
}
