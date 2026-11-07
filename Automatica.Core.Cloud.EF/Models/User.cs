using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Automatica.Core.Cloud.EF.Models
{
    public enum UserRole
    {
        Nothing,
        Viewer,
        Manager,
        Administrator,
        SystemAdministrator
    }

    public class User
    {
        [Key]
        public Guid ObjId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }

        public string PublisherName { get; set; }

        [JsonIgnore]
        public string PasswordHash { get; set; }
        [JsonIgnore]
        public string Salt { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public bool Enabled { get; set; }
        [JsonIgnore]
        public string ActivationCode { get; set; }

        public UserRole UserRole { get; set; }

        /// <summary>
        /// Legacy per-user api key. It is no longer accepted as a credential anywhere (CI uses <see cref="DeployKey"/>).
        /// </summary>
        [JsonIgnore]
        public Guid ApiKey { get; set; }

        /// <summary>
        /// Changes whenever the password, role or enabled state changes. Access and refresh tokens carry it,
        /// so changing it revokes every token issued before.
        /// </summary>
        [JsonIgnore]
        public string SecurityStamp { get; set; }

        public DateTime? LastLoginAt { get; set; }

        [JsonIgnore]
        public int FailedLoginCount { get; set; }

        [JsonIgnore]
        public DateTime? LockedUntil { get; set; }

        [NotMapped]
        [JsonIgnore]
        public bool HasUsablePassword => PasswordHashing.IsUsable(PasswordHash);

        public void RenewSecurityStamp()
        {
            SecurityStamp = Guid.NewGuid().ToString("N");
        }
    }
}
