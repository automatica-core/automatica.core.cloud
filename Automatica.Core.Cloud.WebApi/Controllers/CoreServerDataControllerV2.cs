using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.RemoteControl;
using Automatica.Core.Cloud.WebApi.Authentication;
using Automatica.Core.Cloud.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServerVersion = Automatica.Core.Cloud.EF.Models.ServerVersion;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    /// <summary>
    /// The api every current core calls: webapi/v2/coreServerData/.../{apiKey}/{serverGuid}.
    /// Routes and payloads are unchanged; the core is authenticated by <see cref="CoreServerApiKeyFilter"/>.
    /// </summary>
    [AllowAnonymous, Route("webapi/v{version:apiVersion}/coreServerData"), CoreServerApiKey(requireServerGuid: true), ApiVersion("2.0")]
    [EnableRateLimiting(RateLimits.CoreApi)]
    public class CoreServerDataControllerV2 : BaseController
    {
        private const int MaxRecipientsPerMail = 10;

        private readonly CoreContext _db;
        private readonly IDnsManager _dnsManager;
        private readonly IMailSender _mail;
        private readonly IConfiguration _config;
        private readonly ILogger<CoreServerDataControllerV2> _logger;

        public CoreServerDataControllerV2(CoreContext db, IDnsManager dnsManager, IMailSender mail, IConfiguration config, ILogger<CoreServerDataControllerV2> logger)
        {
            _db = db;
            _dnsManager = dnsManager;
            _mail = mail;
            _config = config;
            _logger = logger;
        }

        private CoreServer Server => HttpContext.GetCoreServer();

        private Task<License> GetLicenseAsync()
        {
            var serverId = Server.ObjId;
            return _db.Licenses.Where(a => a.This2CoreServer == serverId).OrderByDescending(a => a.ExpiresAt).FirstOrDefaultAsync();
        }

        [HttpGet, Route("checkLicense/{apiKey}/{serverGuid}")]
        public string CheckLicense()
        {
            return "INVALID LICENSE";
        }

        [HttpGet, Route("license/{apiKey}/{serverGuid}")]
        public async Task<string> GetLicenseForServer()
        {
            var license = await GetLicenseAsync();
            if (license == null)
            {
                throw new ArgumentException("No License available!");
            }
            return license.LicenseKey;
        }

        [HttpPost, Route("sayHello/{apiKey}/{serverGuid}")]
        public async Task SayHello([FromBody] SayHelloData sayHelloData)
        {
            var server = Server;
            server.LastKnownConnection = DateTime.Now;
            server.Rid = sayHelloData?.Rid;
            server.Version = sayHelloData?.Version;
            await _db.SaveChangesAsync();
        }

        [HttpPost, Route("createRemoteConnectPort/{apiKey}/{serverGuid}")]
        public async Task<RemoteConnectPortResponse> CreateRemoteConnectPort([FromBody] CreateRemoteConnectPortObject createRemoteConnectPortObject)
        {
            var server = Server;
            var license = await GetLicenseAsync();
            if (license is not { AllowRemoteControl: true })
            {
                throw new ArgumentException("No license or invalid license found!");
            }

            var port = await _db.RemoteControlPorts.FirstOrDefaultAsync(a => a.This2CoreServer == server.ObjId && a.This2DriverId == createRemoteConnectPortObject.DriverId);

            var ret = new RemoteConnectPortResponse();
            if (port == null)
            {
                var newPort = new RemoteControlPort
                {
                    This2CoreServer = server.ObjId,
                    This2DriverId = createRemoteConnectPortObject.DriverId,
                    ServiceName = createRemoteConnectPortObject.ServiceName,
                    LastUsed = DateTime.Now,
                    PortType = createRemoteConnectPortObject.TunnelingProtocol
                };

                var entity = _db.RemoteControlPorts.Add(newPort);
                await _db.SaveChangesAsync();
                ret.Port = entity.Entity.Port;
            }
            else
            {
                ret.Port = port.Port;
                port.LastUsed = DateTime.Now;
                await _db.SaveChangesAsync();
            }

            return ret;
        }

        [HttpPost, Route("createRemoteConnect/{apiKey}/{serverGuid}")]
        public Task<RemoteConnectObject> CreateRemoteUrl([FromBody] CreateRemoteConnectObject createRemoteConnectObject)
        {
            return CreateRemoteUrl(createRemoteConnectObject, null);
        }

        [HttpPost, Route("createRemoteConnect/{pluginGuid}/{apiKey}/{serverGuid}")]
        public async Task<RemoteConnectObject> CreateRemoteUrl([FromBody] CreateRemoteConnectObject createRemoteConnectObject, Guid? pluginGuid)
        {
            var server = Server;
            var license = await GetLicenseAsync();
            if (license is not { AllowRemoteControl: true })
            {
                throw new ArgumentException("No license or invalid license found!");
            }

            var target = SubDomainName.Normalize(createRemoteConnectObject?.TargetSubDomain);
            if (!SubDomainName.IsValid(target))
            {
                throw new ArgumentException("Invalid sub domain name");
            }

            var allDomains = await _db.RemoteControlSubDomains.Where(a => a.This2CoreServer == server.ObjId).AsNoTracking().ToListAsync();
            if (allDomains.Count > license.MaxRemoteTunnels)
            {
                throw new ArgumentException("Max allowed licenses reached...");
            }

            var domains = await _db.RemoteControlSubDomains.Where(a => a.This2CoreServer == server.ObjId && a.PluginGuid == pluginGuid).ToListAsync();

            var isDnsAvailable = await _dnsManager.IsDnsNameAvailableAsync(target, server.ObjId, pluginGuid);
            if (!isDnsAvailable)
            {
                throw new ArgumentException("DNS Name is not available!");
            }

            var alreadyExist = false;
            foreach (var domain in domains)
            {
                if (string.Equals(domain.SubDomain, target, StringComparison.OrdinalIgnoreCase))
                {
                    domain.LastUsed = DateTime.Now;
                    domain.PluginGuid = pluginGuid;
                    alreadyExist = true;
                }
                else
                {
                    await _dnsManager.RemoveDnsNameAsync(domain.SubDomain, server.ObjId, pluginGuid);
                    _db.Remove(domain);
                }
            }

            var targetUrl = await _dnsManager.CreateDnsNameAsync(target, server.ObjId, pluginGuid);
            var remoteConnectUrl = new RemoteConnectObject
            {
                TunnelUrl = targetUrl.url,
                SubDomain = target,
                FrpToken = string.IsNullOrEmpty(_config["Frps:Token"]) ? null : _config["Frps:Token"]
            };

            if (!pluginGuid.HasValue)
            {
                server.LastKnownRemoteConnectUrlDate = DateTime.Now;
                server.LastKnownRemoteConnectUrl = remoteConnectUrl.TunnelUrl;
            }

            if (!alreadyExist)
            {
                _db.Add(new RemoteControlSubDomain
                {
                    Domain = "",
                    LastUsed = DateTime.Now,
                    ObjId = Guid.NewGuid(),
                    This2CoreServer = server.ObjId,
                    PluginGuid = pluginGuid,
                    SubDomain = target
                });
            }

            await _db.SaveChangesAsync();
            return remoteConnectUrl;
        }

        [HttpPost, Route("remoteConnect/{apiKey}/{serverGuid}")]
        public async Task<RemoteConnectObject> SetRemoteConnectUrl([FromBody] RemoteConnectObject remoteConnectObj)
        {
            var server = Server;
            var url = remoteConnectObj?.TunnelUrl;
            if (url != null && url.Length > 500)
            {
                throw new ArgumentException("Invalid url");
            }
            server.LastKnownRemoteConnectUrlDate = DateTime.Now;
            server.LastKnownRemoteConnectUrl = url;
            await _db.SaveChangesAsync();

            return remoteConnectObj;
        }

        /// <summary>
        /// Sends a notification from the core. Only goes to the server owner's address and recipients verified in
        /// the cloud UI, as plain text inside one fixed template, rate limited per server.
        /// </summary>
        [HttpPost, Route("sendMail/{apiKey}/{serverGuid}"), EnableRateLimiting(RateLimits.Mail)]
        public async Task<string> SendMail([FromBody] EmailData emailData)
        {
            var server = Server;
            var license = await GetLicenseAsync();
            if (license == null)
            {
                throw new ArgumentException("No license or invalid license found!");
            }

            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var owner = await _db.Users.AsNoTracking().SingleOrDefaultAsync(a => a.ObjId == server.This2User);
            if (!string.IsNullOrEmpty(owner?.Email))
            {
                allowed.Add(owner.Email.Trim());
            }
            var verified = await _db.MailRecipients.AsNoTracking()
                .Where(a => a.This2CoreServer == server.ObjId && a.VerifiedAt != null)
                .Select(a => a.Email).ToListAsync();
            foreach (var address in verified)
            {
                allowed.Add(address.Trim());
            }

            var requested = (emailData?.To ?? new List<string>()).Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var recipients = requested.Where(allowed.Contains).Take(MaxRecipientsPerMail).ToList();
            var skipped = requested.Where(a => !allowed.Contains(a)).ToList();

            if (skipped.Count > 0)
            {
                _logger.LogWarning("Core server {ServerId} tried to mail {Count} unverified recipients", server.ObjId, skipped.Count);
            }

            var subject = MailTemplates.SanitizeSubject(emailData?.Subject);
            var text = MailTemplates.ToPlainText(emailData?.Body);
            var html = MailTemplates.CoreMessageHtml(server.ServerName, text);

            foreach (var to in recipients)
            {
                await _mail.SendAsync(to, subject, text, html);
            }

            return Newtonsoft.Json.JsonConvert.SerializeObject(new { Result = recipients.Count > 0, Skipped = skipped });
        }

        [HttpGet, Route("checkForUpdates/{rid}/{coreServerVersion}/{apiKey}/{serverGuid}")]
        public Task<ServerVersion> CheckForUpdates(string rid, string coreServerVersion)
        {
            return CheckForUpdates(rid, coreServerVersion, "develop");
        }

        [HttpGet, Route("checkForUpdates/{rid}/{coreServerVersion}/{branch}/{apiKey}/{serverGuid}")]
        public async Task<ServerVersion> CheckForUpdates(string rid, string coreServerVersion, string branch)
        {
            var versionObj = new Version(coreServerVersion);
            var versions = (await _db.Versions.Where(a => a.Rid == rid && a.Branch == branch).ToListAsync())
                .Where(a => a.VersionObj > versionObj).OrderByDescending(a => a.VersionObj).ToList();

            return versions.Count > 0 ? versions[0] : null;
        }

        [HttpGet, Route("checkForDockerUpdates/{branch}/{coreServerVersion}/{apiKey}/{serverGuid}")]
        public async Task<ServerDockerVersion> CheckForDockerUpdates(string coreServerVersion, string branch)
        {
            var versionObj = new Version(coreServerVersion);
            var versions = (await _db.DockerVersions.Where(a => a.Branch == branch).ToListAsync())
                .Where(a => a.VersionObj > versionObj).OrderByDescending(a => a.VersionObj).ToList();

            return versions.Count > 0 ? versions[0] : null;
        }

        [HttpGet, Route("plugins/{coreServerVersion}/{branch}/{apiKey}/{serverGuid}")]
        public IEnumerable<Plugin> GetAvailablePlugins(string coreServerVersion, string branch)
        {
            var versionObj = new Version(coreServerVersion);
            var versions = _db.Plugins.AsNoTracking().Where(a => a.Branch == branch).ToList()
                .Where(a => a.MinCoreServerVersionObj != null && versionObj >= a.MinCoreServerVersionObj).ToList();

            return from r in versions
                   group r by r.PluginGuid into g
                   select g.OrderByDescending(x => x.VersionObj).First();
        }

        [HttpGet, Route("plugins/{coreServerVersion}/{apiKey}/{serverGuid}")]
        public IEnumerable<Plugin> GetAvailablePlugins(string coreServerVersion)
        {
            return GetAvailablePlugins(coreServerVersion, "develop");
        }

        [HttpGet, Route("ping/{apiKey}/{serverGuid}")]
        public string Ping()
        {
            return "{\"Response\": \"pong\"}";
        }
    }
}
