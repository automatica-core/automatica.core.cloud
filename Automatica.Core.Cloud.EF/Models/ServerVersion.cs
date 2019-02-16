using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Automatica.Core.Cloud.EF.Models
{
    public class ServerVersion : BaseModel
    {
        public Guid ObjId { get; set; }

        public string Version { get; set; }

        [NotMapped]
        public Version VersionObj
        {
            get
            {
                return new Version(Version);
            }
        }

        public string AzureUrl { get; set; }
        public string AzureFileName { get; set; }

        public string ChangeLog { get; set; }

        public bool IsPrerelease { get; set; }

        public bool IsPublic { get; set; }

        public string Rid { get; set; }
    }
}
