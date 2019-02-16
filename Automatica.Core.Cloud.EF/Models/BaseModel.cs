using System.ComponentModel.DataAnnotations.Schema;

namespace Automatica.Core.Cloud.EF.Models
{
    public class BaseModel
    {
        [NotMapped]
        public bool IsNewObject { get; set; } = false;
    }
}
