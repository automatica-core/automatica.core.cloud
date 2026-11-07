using System;

namespace Automatica.Core.Cloud.EF.Models
{
    /// <summary>
    /// An extra address a core server may send mails to (besides its owner's address), once verified.
    /// </summary>
    public class MailRecipient
    {
        public Guid ObjId { get; set; }
        public Guid This2CoreServer { get; set; }
        public CoreServer This2CoreServerNavigation { get; set; }

        public string Email { get; set; }

        public byte[] VerificationTokenHash { get; set; }
        public DateTime? VerificationExpiresAt { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
