using System;

namespace Automatica.Core.Cloud.EF.Models
{
    public class RemoteControlPort : BaseModel
    {
        public int Port { get; set; }
        public string ServiceName { get; set; }
        public Guid This2CoreServer { get; set; }
        
        public DateTime LastUsed { get; set; }
        public string PortType { get; set; }

        public Guid This2DriverId { get; set; }

        public CoreServer This2CoreServerNavigation { get; set; }
    }
}
