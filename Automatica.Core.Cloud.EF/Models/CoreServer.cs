using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Automatica.Core.Cloud.EF.Models
{
    public class CoreServer : BaseModel
    {
        public Guid ObjId { get; set; }

        public Guid ServerGuid { get; set; }

        public string ServerName { get; set; }

        public DateTime? LastKnownConnection { get; set; }
        public string LastKnownRemoteConnectUrl { get; set; }
        public DateTime? LastKnownRemoteConnectUrlDate { get; set; }

        public string Version { get; set; }

        [NotMapped]
        public Version VersionObj
        {
            get
            {
                if (!string.IsNullOrEmpty(Version))
                {
                    return new Version(Version);
                }
                return null;
            }
        }

        public string Rid { get; set; }

        public Guid ApiKey { get; set; }


        public Guid This2User { get; set; }
        public User This2UserNavigation { get; set; }
    }
}
