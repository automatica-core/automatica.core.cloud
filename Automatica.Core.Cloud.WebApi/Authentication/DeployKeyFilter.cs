using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Cloud.EF.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Cloud.WebApi.Authentication
{
    /// <summary>The deploy key scope an automatica-cli action needs.</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RequireDeployScopeAttribute : Attribute
    {
        public RequireDeployScopeAttribute(DeployKeyScope scope)
        {
            Scope = scope;
        }

        public DeployKeyScope Scope { get; }
    }

    public sealed class DeployKeyAttribute : TypeFilterAttribute
    {
        public DeployKeyAttribute() : base(typeof(DeployKeyFilter))
        {
        }
    }

    /// <summary>
    /// Authenticates CI with a scoped deploy key, sent as header X-Automatica-Deploy-Key or (for the existing
    /// automatica-cli) as the {apiKey} route value. User api keys are not accepted any more.
    /// </summary>
    public sealed class DeployKeyFilter : IAsyncAuthorizationFilter
    {
        public const string HeaderName = "X-Automatica-Deploy-Key";
        public const string ItemKey = "automatica.deployKey";
        public const string KeyPrefix = "acdk_";

        private readonly CoreContext _db;
        private readonly ILogger<DeployKeyFilter> _logger;

        public DeployKeyFilter(CoreContext db, ILogger<DeployKeyFilter> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var http = context.HttpContext;
            string raw = http.Request.Headers[HeaderName];
            if (string.IsNullOrEmpty(raw))
            {
                raw = http.Request.RouteValues["apiKey"]?.ToString();
            }

            if (string.IsNullOrEmpty(raw) || !raw.StartsWith(KeyPrefix, StringComparison.Ordinal) || raw.Length > 128)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }

            var hash = SecretHashing.Sha256(raw);
            var key = (await _db.DeployKeys.Where(a => a.KeyHash == hash).ToListAsync(http.RequestAborted))
                .FirstOrDefault(a => SecretHashing.FixedTimeEquals(a.KeyHash, hash));

            if (key == null || key.RevokedAt.HasValue || (key.ExpiresAt.HasValue && key.ExpiresAt <= DateTime.UtcNow))
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }

            var required = DeployKeyScope.None;
            if (context.ActionDescriptor is ControllerActionDescriptor action)
            {
                var attribute = action.MethodInfo.GetCustomAttributes(typeof(RequireDeployScopeAttribute), false)
                    .OfType<RequireDeployScopeAttribute>().FirstOrDefault();
                if (attribute == null)
                {
                    // every cli action must declare its scope
                    context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                    return;
                }
                required = attribute.Scope;
            }

            if ((key.Scopes & required) != required)
            {
                _logger.LogWarning("Deploy key {KeyId} lacks scope {Scope}", key.ObjId, required);
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }

            var branch = http.Request.RouteValues["branch"]?.ToString() ?? "develop";
            if (!IsBranchAllowed(key.AllowedBranches, branch))
            {
                _logger.LogWarning("Deploy key {KeyId} may not deploy to branch {Branch}", key.ObjId, branch);
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }

            key.LastUsedAt = DateTime.UtcNow;
            var ip = http.Connection.RemoteIpAddress?.ToString();
            key.LastUsedIp = ip?.Length > 64 ? ip[..64] : ip;
            await _db.SaveChangesAsync(http.RequestAborted);

            http.Items[ItemKey] = key;
        }

        internal static bool IsBranchAllowed(string allowed, string branch)
        {
            if (string.IsNullOrWhiteSpace(allowed))
            {
                return false;
            }
            return allowed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(a => a == "*" || string.Equals(a, branch, StringComparison.Ordinal));
        }
    }

    public static class DeployKeyHttpContextExtensions
    {
        public static DeployKey GetDeployKey(this HttpContext context)
        {
            return context.Items[DeployKeyFilter.ItemKey] as DeployKey
                   ?? throw new InvalidOperationException("No authenticated deploy key");
        }
    }
}
