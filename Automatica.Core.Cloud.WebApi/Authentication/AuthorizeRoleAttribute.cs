using Automatica.Core.Cloud.EF.Models;
using Microsoft.AspNetCore.Authorization;

namespace Automatica.Core.Cloud.WebApi.Authentication
{
    public class AuthorizeRoleAttribute : AuthorizeAttribute
    {
        private UserRole _role;

        public AuthorizeRoleAttribute()
        {

        }

        public UserRole Role { get => _role;
            set {
                _role = value;
                Roles = value.ToString();
            } }
    }
}
