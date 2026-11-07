using System;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

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
                    var plain = Version.Split('+')[0];
                    return System.Version.TryParse(plain, out var v) ? v : null;
                }
                return null;
            }
        }

        public string Rid { get; set; }

        /// <summary>
        /// Legacy plaintext api key. Only read to back-fill <see cref="ApiKeyHash"/>; new and rotated keys
        /// leave it at Guid.Empty. Drop the column once the old cloud build is gone for good.
        /// </summary>
        [JsonIgnore]
        public Guid ApiKey { get; set; }

        /// <summary>SHA-256 of the api key (see <see cref="SecretHashing.HashApiKey"/>).</summary>
        [JsonIgnore]
        public byte[] ApiKeyHash { get; set; }

        public DateTime? ApiKeyCreatedAt { get; set; }

        public Guid This2User { get; set; }

        [JsonIgnore]
        public User This2UserNavigation { get; set; }
    }
}
