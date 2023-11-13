using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.WebApi.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    public class RemoteConnectPortResponse
    {
        private int _port;

        public int Port
        {
            get => _port + 1023;
            set => _port = value;
        }
    }
    public class CreateRemoteConnectPortObject
    {
        public Guid DriverId { get; set; }
        public string ServiceName { get; set; }
        public string TunnelingProtocol { get; set; }
    }
    public class CreateRemoteConnectObject
    {
        public string TargetSubDomain { get; set; }
    }
    public class RemoteConnectObject
    {
        public string SubDomain { get; set; }
        public string TunnelUrl { get; set; }
    }

    public class SayHelloData
    {
        public string Rid { get; set; }
        public string Version { get; set; }
        public Guid ServerGuid { get; set; }
    }

    public class EmailData
    {
        public IList<string> To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }

    [AllowAnonymous, Route("webapi/v1/coreServerData"), ServerApiKeyAuthorization, ApiVersion("1.0")]
    public class CoreServerDataController : AzureStorageController
    {

        public CoreServerDataController(IConfiguration config) : base(config)
        {
        }

        [HttpGet, Route("checkLicense/{apiKey}")]
        public string CheckLicense()
        {
            return "INVALID LICENSE";
        }

        [HttpPost, Route("sayHello/{apiKey}")]
        public void SayHello([FromBody]SayHelloData sayHelloData, Guid apiKey)
        {
            var versionObj = new Version(sayHelloData.Version);
            if (versionObj >= new Version(0, 6))
            {
                return;
            }

            using var dbContext = new CoreContext(Config);
            var server = dbContext.CoreServers.SingleOrDefault(a => a.ApiKey == apiKey);

            if (server == null)
            {
                throw new ArgumentException("Api key invalid");
            }

            server.LastKnownConnection = DateTime.Now;
            server.Rid = sayHelloData.Rid;
            server.ServerGuid = sayHelloData.ServerGuid;
            server.Version = sayHelloData.Version;

            dbContext.Update(server);
            dbContext.SaveChanges();
        }

        [HttpPost, Route("sendMail/{apiKey}")]
        public async Task<string> SendMail([FromBody] EmailData emailData, Guid apiKey)
        {
            using (var dbContext = new CoreContext(Config))
            {
                var server = dbContext.CoreServers.SingleOrDefault(a => a.ApiKey == apiKey);
                if (server == null || server.VersionObj >= new Version(0, 6))
                {
                    return "{\"Result\": false}";
                }
            }

            var msg = new SendGridMessage();

            msg.SetFrom(new EmailAddress("cloud@automaticacore.com", "Automatica.Core"));

            foreach (var to in emailData.To)
            {
                msg.AddTo(new EmailAddress(to));
            }

            msg.SetSubject(emailData.Subject);
            msg.AddContent(MimeType.Text, emailData.Body);

            var sendGridApiKey = Config.GetSection("SENDGRID_API_KEY").Value;
            var client = new SendGridClient(sendGridApiKey);

            var response = await client.SendEmailAsync(msg);

            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                return "{\"Result\": true}";
            }

            return "{\"Result\": false}";

        }

        [HttpGet, Route("checkForUpdates/{rid}/{coreServerVersion}/{apiKey}")]
        public ServerVersion CheckForUpdates(string rid, string coreServerVersion)
        {
            return CheckForUpdates(rid, coreServerVersion, "develop");
        }

        [HttpGet, Route("checkForUpdates/{rid}/{coreServerVersion}/{branch}/{apiKey}")]
        public ServerVersion CheckForUpdates(string rid, string coreServerVersion, string branch)
        {
            var versionObj = new Version(coreServerVersion);
            if (versionObj >= new Version(0, 6))
            {
                return null;
            }

            using var dbContext = new CoreContext(Config);
            var versions = dbContext.Versions.Where(a => a.VersionObj > versionObj && a.Rid == rid && a.Branch == branch).OrderByDescending(a => a.VersionObj).ToList();

            if (versions.Count > 0)
            {
                return versions[0];
            }
            return null;
        }

        [HttpGet, Route("plugins/{coreServerVersion}/{apiKey}")]
        public IEnumerable<Plugin>    GetAvailablePlugins(string coreServerVersion)
        {
            var versionObj = new Version(coreServerVersion);
            if (versionObj >= new Version(0, 6))
            {
                return null;
            }

            using var dbContext = new CoreContext(Config);
            var versions = dbContext.Plugins.Where(a => versionObj >= a.MinCoreServerVersionObj).ToList();
            return from r in versions
                group r by r.PluginGuid into g
                select g.OrderByDescending(x_ => x_.VersionObj).First();
        }

        [HttpGet, Route("ping/{apiKey}")]
        public string Ping(Guid apiKey)
        {
            using var dbContext = new CoreContext(Config);
            var server = dbContext.CoreServers.SingleOrDefault(a => a.ApiKey == apiKey);
            if (server == null || server.VersionObj >= new Version(0, 6))
            {
                return null;
            }

            return "{\"Response\": \"pong\"}";
        }
    }
}
