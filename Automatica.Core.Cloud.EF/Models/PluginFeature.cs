using System;
using System.Collections.Generic;
using System.Text;

namespace Automatica.Core.Cloud.EF.Models
{
    public class PluginFeature
    {

        public Guid ObjId { get; set; }
        public Guid This2Plugin { get; set; }
        public string FeatureName { get; set; }
        public bool IsMandatory { get; set; }
        public Plugin This2PluginNavigation { get; set; }
    }
}
