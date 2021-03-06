﻿using System;

namespace Automatica.Core.Cloud.EF.Models
{
    public class License : BaseModel
    {
        public Guid ObjId { get; set; }

        public Guid This2CoreServer { get; set; }

        public string LicenseKey { get; set; }

        public Guid This2VersionKey { get; set; }

        public int MaxDatapoints { get; set; }
        public int MaxUsers { get; set; }

        public string LicensedTo { get; set; }
        public string Email { get; set; }

        public CoreServer This2CoreServerNavigation { get; set; }

        public LicenseKey This2VersionKeyNavigation { get; set; }

        public string FeaturesString { get; set; }
    }
}
