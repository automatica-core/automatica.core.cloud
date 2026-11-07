using System;

namespace Automatica.Core.Cloud.EF.Models
{
    public class RefreshToken
    {
        public Guid ObjId { get; set; }
        public Guid This2User { get; set; }
        public User This2UserNavigation { get; set; }

        public byte[] TokenHash { get; set; }

        /// <summary>All tokens of one login share a family; reusing a rotated token revokes the whole family.</summary>
        public Guid FamilyId { get; set; }

        public string SecurityStamp { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public Guid? ReplacedBy { get; set; }
        public string CreatedByIp { get; set; }
    }
}
