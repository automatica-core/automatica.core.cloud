using System;

namespace Automatica.Core.Cloud.EF.Models
{
    public class License : BaseModel
    {
        public Guid ObjId { get; set; }

        public Guid This2CoreServer { get; set; }
        
        public DateTime ExpiresAt { get; set; }
        public string LicenseKey { get; set; }

        public Guid This2VersionKey { get; set; }

        public int MaxDatapoints { get; set; }
        public int MaxUsers { get; set; }

        public string LicensedTo { get; set; }
        public string Email { get; set; }

        public bool AllowRemoteControl { get; set; }

        public int MaxRemoteTunnels { get; set; }

        public long MaxRecordingDataPoints { get; set; }
        public int MaxSatellites { get; set; }

        public bool AllowTextToSpeech { get; set; }

        public CoreServer This2CoreServerNavigation { get; set; }

        public LicenseKey This2VersionKeyNavigation { get; set; }

        public string FeaturesString { get; set; }
    }
}
