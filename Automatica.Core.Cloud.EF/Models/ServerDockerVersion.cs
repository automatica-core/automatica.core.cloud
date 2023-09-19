using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Automatica.Core.Cloud.EF.Models
{
    public class ServerDockerVersion : BaseModel
    {
        public Guid ObjId { get; set; }

        public string Version { get; set; }

        [NotMapped]
        public Version VersionObj => new(Version);

        public string ImageName { get; set; }
        public string ImageTag { get; set; }

        public string ChangeLog { get; set; }

        public bool IsPreRelease { get; set; }

        public bool IsPublic { get; set; }
        public string Branch { get; set; }
    }
}
