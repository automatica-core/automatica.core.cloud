using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Automatica.Core.Cloud.EF.Models
{
    public enum PluginType
    {
        Driver,
        Rule
    }

    public class Plugin : BaseModel
    {
        public Plugin()
        {
            LicenseFeatures = new List<PluginFeature>();
        }

        public Guid ObjId { get; set; }

        public Guid PluginGuid { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public PluginType PluginType { get; set; }

        public Guid This2User { get; set; }

        public string Publisher { get; set; }

        public string ComponentName { get; set; }

        public string PluginVersion { get; set; }

        [NotMapped]
        public Version PluginVersionObj
        {
            get
            {
                if (!String.IsNullOrEmpty(PluginVersion))
                {
                    return new Version(PluginVersion);
                }

                return new Version();
            }
        }

        [NotMapped]
        public Version VersionObj => new Version(Version);

        public string MinCoreServerVersion { get; set; }

        [NotMapped]
        public Version MinCoreServerVersionObj
        {
            get
            {
                if(string.IsNullOrEmpty(MinCoreServerVersion))
                {
                    return null;
                }
                return new Version(MinCoreServerVersion);
            }
        }


        public string AzureUrl { get; set; }
        public string AzureFileName { get; set; }

        public bool IsPublic { get; set; }
        public bool IsPrerelease { get; set; }
        public User This2UserNavigation { get; internal set; }
        public IList<PluginFeature> LicenseFeatures { get; set; }
    }
}
