using Automatica.Core.Cloud.EF.Models;
using System;

namespace Automatica.Core.Cloud.WebApi.Authentication
{
    public class NeedsRoleAttribute : Attribute
    {
        public NeedsRoleAttribute(UserRole role)
        {
            Role = role;
        }

        public UserRole Role { get; }
    }
}
