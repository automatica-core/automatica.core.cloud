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

    [AllowAnonymous, Route("v{version:apiVersion}/coreServerData"), ServerApiKeyAuthorizationV2, ApiVersion("2.0")]
    public class CoreServerDataControllerV2 : AzureStorageController
    {

        public CoreServerDataControllerV2(IConfiguration config) : base(config)
        {
        }

        [HttpGet, Route("checkLicense/{apiKey}/{serverGuid}")]
        public string CheckLicense()
        {
            return "INVALID LICENSE";
        }

        [HttpPost, Route("sayHello/{apiKey}/{serverGuid}")]
        public void SayHello([FromBody]SayHelloData sayHelloData, Guid apiKey)
        {
            using (var dbContext = new CoreContext(Config))
            {
                var server = dbContext.CoreServers.SingleOrDefault(a => a.ApiKey == apiKey);

                if (server == null)
                {
                    throw new ArgumentException("Api key invalid");
                }

                server.LastKnownConnection = DateTime.Now;
                server.Rid = sayHelloData.Rid;
                server.Version = sayHelloData.Version;

                dbContext.Update(server);
                dbContext.SaveChanges();
            }
        }

        [HttpPost, Route("sendMail/{apiKey}/{serverGuid}")]
        public async Task<string> SendMail([FromBody] EmailData emailData, Guid apiKey)
        {
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

        [HttpGet, Route("checkForUpdates/{rid}/{coreServerVersion}/{apiKey}/{serverGuid}")]
        public ServerVersion CheckForUpdates(string rid, string coreServerVersion)
        {
            using (var dbContext = new CoreContext(Config))
            {
                var versionObj = new Version(coreServerVersion);
                var versions = dbContext.Versions.Where(a => a.VersionObj > versionObj && a.Rid == rid).OrderByDescending(a => a.VersionObj).ToList();

                if (versions.Count > 0)
                {
                    return versions[0];
                }
                return null;
            }
        }

        [HttpGet, Route("plugins/{coreServerVersion}/{branch}/{apiKey}/{serverGuid}")]
        public IEnumerable<Plugin> GetAvailablePlugins(string coreServerVersion, string branch)
        {
            using (var dbContext = new CoreContext(Config))
            {
                var versionObj = new Version(coreServerVersion);
                var versions = dbContext.Plugins.Where(a => versionObj >= a.MinCoreServerVersionObj && a.Branch == branch).ToList();

                return from r in versions
                    group r by r.PluginGuid into g
                    select g.OrderByDescending(x => x.VersionObj).First();
            }
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
