using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.WebApi.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    /// <summary>
    /// frps HTTP server plugin (frp "manage" plugin protocol) for the Login and NewProxy operations.
    /// frps calls it with a shared secret in the path; the cloud decides whether a core may log in and which
    /// subdomains / remote ports it may open. frpc sends <c>user = ServerGuid</c> (FRPC_USERNAME in the core).
    /// </summary>
    [AllowAnonymous, ApiController, Route("webapi/v1/frps/handler/{secret}")]
    [EnableRateLimiting(RateLimits.Frps)]
    public class FrpsPluginController : ControllerBase
    {
        private readonly CoreContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<FrpsPluginController> _logger;

        public FrpsPluginController(CoreContext db, IConfiguration config, ILogger<FrpsPluginController> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        private bool RequireApiKeyMeta => _config.GetValue("Frps:RequireApiKeyMeta", false);

        [HttpPost, Route("")]
        public async Task<IActionResult> Handle([FromRoute] string secret, [FromQuery] string op, [FromBody] JObject request)
        {
            var expected = _config["Frps:PluginSecret"];
            if (string.IsNullOrEmpty(expected) ||
                !SecretHashing.FixedTimeEquals(Encoding.UTF8.GetBytes(secret ?? string.Empty), Encoding.UTF8.GetBytes(expected)))
            {
                return NotFound();
            }

            op ??= request?.Value<string>("op");
            var content = request?["content"] as JObject;
            if (content == null)
            {
                return Reject("invalid request");
            }

            return op switch
            {
                "Login" => await LoginAsync(content),
                "NewProxy" => await NewProxyAsync(content),
                _ => Allow()
            };
        }

        private async Task<IActionResult> LoginAsync(JObject content)
        {
            var server = await FindServerAsync(content.Value<string>("user"), content["metas"] as JObject);
            return server == null ? Reject("unknown client") : Allow();
        }

        private async Task<IActionResult> NewProxyAsync(JObject content)
        {
            var user = content["user"] as JObject;
            var server = await FindServerAsync(user?.Value<string>("user"), user?["metas"] as JObject);
            if (server == null)
            {
                return Reject("unknown client");
            }

            var proxyType = content.Value<string>("proxy_type")?.ToLowerInvariant();
            switch (proxyType)
            {
                case "http":
                case "https":
                {
                    if (content["custom_domains"] is JArray { Count: > 0 })
                    {
                        return Reject("custom domains are not allowed");
                    }

                    var subDomain = content.Value<string>("subdomain");
                    if (string.IsNullOrEmpty(subDomain))
                    {
                        return Reject("subdomain required");
                    }

                    var owned = await _db.RemoteControlSubDomains.AsNoTracking()
                        .AnyAsync(a => a.This2CoreServer == server.ObjId && a.SubDomain == subDomain.ToLower());
                    return owned ? Allow() : Reject("subdomain not assigned to this server");
                }
                case "tcp":
                case "udp":
                {
                    var remotePort = content.Value<int?>("remote_port") ?? 0;
                    var portId = remotePort - 1023;
                    var owned = portId > 0 && await _db.RemoteControlPorts.AsNoTracking()
                        .AnyAsync(a => a.This2CoreServer == server.ObjId && a.Port == portId);
                    return owned ? Allow() : Reject("port not assigned to this server");
                }
                default:
                    return Reject("proxy type not allowed");
            }
        }

        private async Task<CoreServer> FindServerAsync(string user, JObject metas)
        {
            if (!Guid.TryParse(user, out var serverGuid))
            {
                return null;
            }

            var candidates = await _db.CoreServers.AsNoTracking().Where(a => a.ServerGuid == serverGuid).ToListAsync();
            if (candidates.Count == 0)
            {
                return null;
            }

            var apiKey = metas?.Value<string>("apikey");
            if (!string.IsNullOrEmpty(apiKey))
            {
                if (!Guid.TryParse(apiKey, out var key))
                {
                    return null;
                }
                var hash = SecretHashing.HashApiKey(key);
                return candidates.FirstOrDefault(a => a.ApiKeyHash != null && SecretHashing.FixedTimeEquals(a.ApiKeyHash, hash));
            }

            if (RequireApiKeyMeta)
            {
                return null;
            }

            if (candidates.Count > 1)
            {
                _logger.LogWarning("frps login for server guid {ServerGuid} matches {Count} servers", serverGuid, candidates.Count);
            }
            return candidates[0];
        }

        private static IActionResult Allow() => new ContentResult
        {
            Content = JsonConvert.SerializeObject(new Dictionary<string, object> { ["reject"] = false, ["unchange"] = true }),
            ContentType = "application/json",
            StatusCode = 200
        };

        private IActionResult Reject(string reason)
        {
            _logger.LogInformation("frps request rejected: {Reason}", reason);
            return new ContentResult
            {
                Content = JsonConvert.SerializeObject(new Dictionary<string, object> { ["reject"] = true, ["reject_reason"] = reason }),
                ContentType = "application/json",
                StatusCode = 200
            };
        }
    }
}
