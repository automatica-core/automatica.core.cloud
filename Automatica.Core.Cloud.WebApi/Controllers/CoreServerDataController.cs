using System;
using System.Collections.Generic;
using System.Linq;
using Asp.Versioning;
using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.WebApi.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    /// <summary>
    /// Legacy v1 api for cores older than 0.6 (authenticated by api key only). Routes and payloads are unchanged.
    /// </summary>
    [AllowAnonymous, Route("webapi/v1/coreServerData"), CoreServerApiKey(requireServerGuid: false), ApiVersion("1.0")]
    [EnableRateLimiting(RateLimits.CoreApi)]
    public class CoreServerDataController : BaseController
    {
        private static readonly Version LegacyLimit = new(0, 6);
        private readonly CoreContext _db;

        public CoreServerDataController(CoreContext db)
        {
            _db = db;
        }

        [HttpGet, Route("checkLicense/{apiKey}")]
        public string CheckLicense()
        {
            return "INVALID LICENSE";
        }

        [HttpPost, Route("sayHello/{apiKey}")]
        public void SayHello([FromBody] SayHelloData sayHelloData)
        {
            if (!Version.TryParse(sayHelloData?.Version, out var versionObj) || versionObj >= LegacyLimit)
            {
                return;
            }

            var server = HttpContext.GetCoreServer();
            server.LastKnownConnection = DateTime.Now;
            server.Rid = sayHelloData.Rid;
            server.ServerGuid = sayHelloData.ServerGuid;
            server.Version = sayHelloData.Version;
            _db.SaveChanges();
        }

        [HttpGet, Route("checkForUpdates/{rid}/{coreServerVersion}/{apiKey}")]
        public ServerVersion CheckForUpdates(string rid, string coreServerVersion)
        {
            return CheckForUpdates(rid, coreServerVersion, "develop");
        }

        [HttpGet, Route("checkForUpdates/{rid}/{coreServerVersion}/{branch}/{apiKey}")]
        public ServerVersion CheckForUpdates(string rid, string coreServerVersion, string branch)
        {
            if (!Version.TryParse(coreServerVersion, out var versionObj) || versionObj >= LegacyLimit)
            {
                return null;
            }

            return _db.Versions.Where(a => a.Rid == rid && a.Branch == branch).ToList()
                .Where(a => a.VersionObj > versionObj)
                .OrderByDescending(a => a.VersionObj)
                .FirstOrDefault();
        }

        [HttpGet, Route("plugins/{coreServerVersion}/{apiKey}")]
        public IEnumerable<Plugin> GetAvailablePlugins(string coreServerVersion)
        {
            if (!Version.TryParse(coreServerVersion, out var versionObj) || versionObj >= LegacyLimit)
            {
                return null;
            }

            var versions = _db.Plugins.ToList().Where(a => a.MinCoreServerVersionObj != null && versionObj >= a.MinCoreServerVersionObj).ToList();
            return from r in versions
                   group r by r.PluginGuid into g
                   select g.OrderByDescending(x => x.VersionObj).First();
        }

        [HttpGet, Route("ping/{apiKey}")]
        public string Ping()
        {
            var server = HttpContext.GetCoreServer();
            if (server.VersionObj >= LegacyLimit)
            {
                return null;
            }

            return "{\"Response\": \"pong\"}";
        }
    }
}
