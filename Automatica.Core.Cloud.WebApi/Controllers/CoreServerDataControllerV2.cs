﻿using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.WebApi.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Automatica.Core.Cloud.RemoteControl;
using Microsoft.EntityFrameworkCore;
using SendGrid;
using SendGrid.Helpers.Mail;
using ServerVersion = Automatica.Core.Cloud.EF.Models.ServerVersion;

namespace Automatica.Core.Cloud.WebApi.Controllers
{

    [AllowAnonymous, Route("webapi/v{version:apiVersion}/coreServerData"), ServerApiKeyAuthorizationV2, ApiVersion("2.0")]
    public class CoreServerDataControllerV2 : AzureStorageController
    {
        private readonly IDnsManager _dnsManager;

        public CoreServerDataControllerV2(IConfiguration config, CoreContext context, IDnsManager dnsManager) : base(config)
        {
            _dnsManager = dnsManager;
        }

        [HttpGet, Route("checkLicense/{apiKey}/{serverGuid}")]
        public string CheckLicense()
        {
            return "INVALID LICENSE";
        }



        [HttpGet, Route("license/{apiKey}/{serverGuid}"), AuthorizeRole(Role = UserRole.SystemAdministrator)]
        public string GetLicenseForServer(Guid apiKey)
        {
            using var dbContext = new CoreContext(Config);
            var server = dbContext.CoreServers.Single(a => a.ApiKey == apiKey);

            if (server == null)
            {
                throw new ArgumentException("Api key invalid");
            }

            var license = dbContext.Licenses.Single(a => a.This2CoreServer == server.ObjId);

            if (license == null)
            {
                throw new ArgumentException("No License available!");
            }

            return license.LicenseKey;
        }

        [HttpPost, Route("sayHello/{apiKey}/{serverGuid}")]
        public void SayHello([FromBody]SayHelloData sayHelloData, Guid apiKey)
        {
            using var dbContext = new CoreContext(Config);
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

        [HttpPost, Route("createRemoteConnectPort/{apiKey}/{serverGuid}")]
        public async Task<RemoteConnectPortResponse> CreateRemoteConnectPort(
            [FromBody] CreateRemoteConnectPortObject createRemoteConnectPortObject, Guid apiKey)
        {
            await using var dbContext = new CoreContext(Config);
            var server = dbContext.CoreServers.SingleOrDefault(a => a.ApiKey == apiKey);

            if (server == null)
            {
                throw new ArgumentException("Api key invalid");
            }

            var license = dbContext.Licenses.SingleOrDefault(a => a.This2CoreServer == server.ObjId);

            if (license is not { AllowRemoteControl: true })
            {
                throw new ArgumentException("No license or invalid license found!");
            }

            var ports = dbContext.RemoteControlPorts.Where(a => a.This2CoreServer == server.ObjId).ToList();
            
            var port = ports.FirstOrDefault(a => a.This2DriverId == createRemoteConnectPortObject.DriverId);

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

                var entity = dbContext.RemoteControlPorts.Add(newPort);
                await dbContext.SaveChangesAsync();

                ret.Port = entity.Entity.Port;
            }
            else
            {
                ret.Port = port.Port;
                port.LastUsed = DateTime.Now;
            }

            return ret;
        }


        [HttpPost, Route("createRemoteConnect/{apiKey}/{serverGuid}")]
        public async Task<RemoteConnectObject> CreateRemoteUrl([FromBody] CreateRemoteConnectObject createRemoteConnectObject, Guid apiKey)
        {
            return await CreateRemoteUrl(createRemoteConnectObject, null, apiKey);
        }

        [HttpPost, Route("createRemoteConnect/{pluginGuid}/{apiKey}/{serverGuid}")]
        public async Task<RemoteConnectObject> CreateRemoteUrl([FromBody] CreateRemoteConnectObject createRemoteConnectObject, Guid? pluginGuid, Guid apiKey)
        {
            await using var dbContext = new CoreContext(Config);
            var server = dbContext.CoreServers.SingleOrDefault(a => a.ApiKey == apiKey);

            if (server == null)
            {
                throw new ArgumentException("Api key invalid");
            }

            var license = dbContext.Licenses.SingleOrDefault(a => a.This2CoreServer == server.ObjId);

            if (license is not { AllowRemoteControl: true })
            {
                throw new ArgumentException("No license or invalid license found!");
            }

            var allDomains = dbContext.RemoteControlSubDomains.Where(a => a.This2CoreServer == server.ObjId).AsNoTracking().ToList();

            if (allDomains.Count > license.MaxRemoteTunnels)
            {
                throw new ArgumentException("Max allowed licenses reached...");
            }

            var domains = dbContext.RemoteControlSubDomains.Where(a => a.This2CoreServer == server.ObjId && a.PluginGuid == pluginGuid).AsNoTracking().ToList();

            var isDnsAvailable = await _dnsManager.IsDnsNameAvailableAsync(createRemoteConnectObject.TargetSubDomain, server.ObjId, pluginGuid);

            if (!isDnsAvailable)
            {
                throw new ArgumentException("DNS Name is not available!");
            }

            bool alreadyExist = false;
            foreach (var domain in domains)
            {
                if (domain.SubDomain == createRemoteConnectObject.TargetSubDomain)
                {
                    var curDomain = dbContext.RemoteControlSubDomains.Single(a => a.ObjId == domain.ObjId);
                    curDomain.LastUsed = DateTime.Now;
                    curDomain.PluginGuid = pluginGuid;
                    dbContext.Update(curDomain);
                    alreadyExist = true;
                }
                else
                {
                    await _dnsManager.RemoveDnsNameAsync(domain.SubDomain, server.ObjId, pluginGuid);
                    dbContext.Remove(domain);
                }
            }

            var targetUrl = await _dnsManager.CreateDnsNameAsync(createRemoteConnectObject.TargetSubDomain, server.ObjId, pluginGuid);
            var remoteConnectUrl = new RemoteConnectObject
            {
                TunnelUrl = targetUrl.url,
                SubDomain = createRemoteConnectObject.TargetSubDomain
            };
            
            if(!pluginGuid.HasValue)
                await SetRemoteConnectUrl(remoteConnectUrl, apiKey);

            if (!alreadyExist)
            {
                var subDomain = new RemoteControlSubDomain
                {
                    Domain = "",
                    LastUsed = DateTime.Now,
                    ObjId = Guid.NewGuid(),
                    This2CoreServer = server.ObjId,
                    PluginGuid = pluginGuid,
                    SubDomain = createRemoteConnectObject.TargetSubDomain
                };
                dbContext.Add(subDomain);
            }

            await dbContext.SaveChangesAsync();

            return remoteConnectUrl;
        }

        [HttpPost, Route("remoteConnect/{apiKey}/{serverGuid}")]
        public async Task<RemoteConnectObject> SetRemoteConnectUrl([FromBody] RemoteConnectObject remoteConnectObj, Guid apiKey)
        {
            await using var dbContext = new CoreContext(Config);
            var server = dbContext.CoreServers.SingleOrDefault(a => a.ApiKey == apiKey);

            if (server == null)
            {
                throw new ArgumentException("Api key invalid");
            }

            server.LastKnownRemoteConnectUrlDate = DateTime.Now;
            server.LastKnownRemoteConnectUrl = remoteConnectObj.TunnelUrl;

            dbContext.Update(server);
            await dbContext.SaveChangesAsync();
            
            return remoteConnectObj;
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
            return CheckForUpdates(rid, coreServerVersion, "develop");
        }

        [HttpGet, Route("checkForUpdates/{rid}/{coreServerVersion}/{branch}/{apiKey}/{serverGuid}")]
        public ServerVersion CheckForUpdates(string rid, string coreServerVersion, string branch)
        {
            using var dbContext = new CoreContext(Config);
            var versionObj = new Version(coreServerVersion);
            var versions = dbContext.Versions.Where(a => a.VersionObj > versionObj && a.Rid == rid && a.Branch == branch).OrderByDescending(a => a.VersionObj).ToList();

            if (versions.Count > 0)
            {
                return versions[0];
            }
            return null;
        }

        [HttpGet, Route("plugins/{coreServerVersion}/{branch}/{apiKey}/{serverGuid}")]
        public IEnumerable<Plugin> GetAvailablePlugins(string coreServerVersion, string branch)
        {
            using var dbContext = new CoreContext(Config);
            var versionObj = new Version(coreServerVersion);
            var versions = dbContext.Plugins.ToList().Where(a => versionObj >= a.MinCoreServerVersionObj && a.Branch == branch).ToList();

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
