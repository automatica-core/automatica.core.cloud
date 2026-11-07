using System;
using System.Security.Claims;
using Automatica.Core.Cloud.EF.Models;

namespace Automatica.Core.Cloud.WebApi.Authentication
{
    public static class Policies
    {
        /// <summary>Administrator or SystemAdministrator: licenses, all servers.</summary>
        public const string Admin = "admin";

        /// <summary>SystemAdministrator: users, plugins, server versions, docker versions, deploy keys.</summary>
        public const string SysAdmin = "sysadmin";

        public const string RoleClaim = "role";
        public const string SubjectClaim = "sub";
        public const string NameClaim = "name";
        public const string StampClaim = "sst";

        public static UserRole GetRole(this ClaimsPrincipal principal)
        {
            var value = principal?.FindFirst(RoleClaim)?.Value;
            return Enum.TryParse<UserRole>(value, out var role) ? role : UserRole.Nothing;
        }

        public static Guid GetUserId(this ClaimsPrincipal principal)
        {
            var value = principal?.FindFirst(SubjectClaim)?.Value;
            return Guid.TryParse(value, out var id) ? id : Guid.Empty;
        }

        public static bool IsAdmin(this ClaimsPrincipal principal)
        {
            return principal.GetRole() >= UserRole.Administrator;
        }

        public static bool IsSysAdmin(this ClaimsPrincipal principal)
        {
            return principal.GetRole() == UserRole.SystemAdministrator;
        }
    }
}
