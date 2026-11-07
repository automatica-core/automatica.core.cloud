using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Automatica.Core.Cloud.RemoteControl
{
    /// <summary>
    /// Validation for sub domains requested by cores. Only a single DNS label is allowed so a core cannot
    /// create records like "@", "www" or nested names inside the zone.
    /// </summary>
    public static partial class SubDomainName
    {
        private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
        {
            "www", "api", "cloud", "mail", "smtp", "frps", "frp", "dashboard", "ns1", "ns2", "autodiscover"
        };

        [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$", RegexOptions.CultureInvariant)]
        private static partial Regex LabelRegex();

        public static bool IsValid(string? name)
        {
            return !string.IsNullOrEmpty(name) && LabelRegex().IsMatch(name) && !Reserved.Contains(name);
        }

        public static string Normalize(string? name)
        {
            return (name ?? string.Empty).Trim().ToLowerInvariant();
        }
    }
}
