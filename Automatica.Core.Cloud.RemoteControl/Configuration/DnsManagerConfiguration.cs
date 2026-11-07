namespace Automatica.Core.Cloud.RemoteControl.Configuration
{
    public class DnsManagerConfiguration
    {
        /// <summary>Subscription of the DNS zone; empty = the identity's default subscription.</summary>
        public string? SubscriptionId { get; set; }

        public string ResourceGroup { get; set; } = "automatica";

        public string DnsZoneName { get; set; } = "automaticaremote.com";

        /// <summary>Host every remote-connect CNAME points at (the frps VM).</summary>
        public string CNameTarget { get; set; } = "dev.automaticaremote.com";

        /// <summary>Suffix inserted between the sub domain and the zone ("foo" -> "foo.dev").</summary>
        public string? CNamePrefix { get; set; } = "dev";
    }
}
