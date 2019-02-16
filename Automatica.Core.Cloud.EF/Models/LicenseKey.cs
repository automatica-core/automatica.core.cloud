using System;

namespace Automatica.Core.Cloud.EF.Models
{
    public class LicenseKey
    {
        public Guid ObjId { get; set; }

        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public int Version { get; set; }
    }
}
