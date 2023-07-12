using System;

namespace Automatica.Core.Cloud.EF.Models
{
    public class RemoteControlSubDomain : BaseModel
    {
        public Guid ObjId { get; set; }
        public string Domain { get; set; }
        public string SubDomain { get; set; }
        public Guid This2CoreServer { get; set; }
        public Guid? PluginGuid { get; set; }
        public DateTime LastUsed { get; set; }
        public CoreServer This2CoreServerNavigation { get; set; }
    }
}
