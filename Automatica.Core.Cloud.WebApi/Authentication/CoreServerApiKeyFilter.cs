using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Cloud.EF.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Automatica.Core.Cloud.WebApi.Authentication
{
    /// <summary>
    /// Authenticates an on-prem core by the {apiKey} (and, for v2, {serverGuid}) route values, or by the
    /// X-Automatica-ApiKey / X-Automatica-ServerGuid headers, which win when both are sent.
    /// The key is looked up by its SHA-256 and compared in constant time; the plaintext key is never queried.
    /// </summary>
    public sealed class CoreServerApiKeyAttribute : TypeFilterAttribute
    {
        public CoreServerApiKeyAttribute(bool requireServerGuid = true) : base(typeof(CoreServerApiKeyFilter))
        {
            Arguments = new object[] { requireServerGuid };
        }
    }

    public sealed class CoreServerApiKeyFilter : IAsyncAuthorizationFilter
    {
        public const string ItemKey = "automatica.coreServer";
        public const string ApiKeyHeader = "X-Automatica-ApiKey";
        public const string ServerGuidHeader = "X-Automatica-ServerGuid";

        private readonly CoreContext _db;
        private readonly bool _requireServerGuid;

        public CoreServerApiKeyFilter(CoreContext db, bool requireServerGuid)
        {
            _db = db;
            _requireServerGuid = requireServerGuid;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var server = await AuthenticateAsync(_db, context.HttpContext, _requireServerGuid);
            if (server == null)
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }

            context.HttpContext.Items[ItemKey] = server;
        }

        internal static async Task<CoreServer> AuthenticateAsync(CoreContext db, HttpContext httpContext, bool requireServerGuid)
        {
            if (!Guid.TryParse(Read(httpContext, ApiKeyHeader, "apiKey"), out var apiKey) || apiKey == Guid.Empty)
            {
                return null;
            }

            Guid serverGuid = Guid.Empty;
            if (requireServerGuid && !Guid.TryParse(Read(httpContext, ServerGuidHeader, "serverGuid"), out serverGuid))
            {
                return null;
            }

            var hash = SecretHashing.HashApiKey(apiKey);
            var candidates = await db.CoreServers.Where(a => a.ApiKeyHash == hash).ToListAsync(httpContext.RequestAborted);

            return candidates.FirstOrDefault(a => SecretHashing.FixedTimeEquals(a.ApiKeyHash, hash) && (!requireServerGuid || a.ServerGuid == serverGuid));
        }

        /// <summary>The header wins over the route value, so a later core can drop the key from the path.</summary>
        private static string Read(HttpContext httpContext, string header, string routeValue)
        {
            var fromHeader = httpContext.Request.Headers[header].ToString();
            return !string.IsNullOrWhiteSpace(fromHeader) ? fromHeader.Trim() : httpContext.Request.RouteValues[routeValue]?.ToString();
        }
    }

    public static class CoreServerHttpContextExtensions
    {
        public static CoreServer GetCoreServer(this HttpContext context)
        {
            return context.Items[CoreServerApiKeyFilter.ItemKey] as CoreServer
                   ?? throw new InvalidOperationException("No authenticated core server");
        }
    }
}
