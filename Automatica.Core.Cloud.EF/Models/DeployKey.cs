using System;

namespace Automatica.Core.Cloud.EF.Models
{
    [Flags]
    public enum DeployKeyScope
    {
        None = 0,
        DeployPlugin = 1,
        DeployUpdate = 2,
        DeployDocker = 4,
        ReadPlugins = 8
    }

    /// <summary>
    /// Scoped credential for CI (automatica-cli). Only the SHA-256 of the key is stored.
    /// </summary>
    public class DeployKey
    {
        public Guid ObjId { get; set; }
        public string Name { get; set; }

        public byte[] KeyHash { get; set; }

        /// <summary>First characters of the key, to recognise it in the UI.</summary>
        public string KeyPrefix { get; set; }

        public DeployKeyScope Scopes { get; set; }

        /// <summary>Comma separated branch names the key may deploy to, "*" for any.</summary>
        public string AllowedBranches { get; set; }

        /// <summary>The user deployed plugins are published as.</summary>
        public Guid This2User { get; set; }
        public User This2UserNavigation { get; set; }

        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public string LastUsedIp { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
