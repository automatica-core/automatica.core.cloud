using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.WebApi.Authentication;
using Automatica.Core.Cloud.WebApi.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    [Route("v{version:apiVersion}/coreCliData"), ApiVersion("1.0")]
    [AllowAnonymous]
    [UserApiKeyAuthorization]
    [NeedsRole(EF.Models.UserRole.SystemAdministrator)]
    [DisableRequestSizeLimit]
    public class CoreCliDataController : AzureStorageController
    {
        public CoreCliDataController(CoreContext dbContext, IConfiguration config, ILogger<CoreCliDataController> logger) : base(config)
        {
            DbContext = dbContext;
            Logger = logger;
        }

        public CoreContext DbContext { get; }
        public ILogger Logger { get; }
        

        [HttpPost, Route("deployPlugin/{deleteOldPackages}/{apiKey}")]
        [DisableRequestSizeLimit]
        [NeedsRole(UserRole.SystemAdministrator)]
        public async Task DeployPluginAndDelete(Guid apiKey, bool deleteOldPackages)
        {
            var container = GetCloudBlobContainer();
            var myFile = Request.Form.Files[0];

            var manifest = await PluginHelper.UploadAndSave(DbContext, Logger, myFile, container, apiKey);

            if(deleteOldPackages)
            {
                // delete only other plugins with the same min coreserver version to stay compatible
                var others = DbContext.Plugins.Where(a => a.VersionObj < manifest.Automatica.PluginVersion && a.MinCoreServerVersionObj == manifest.Automatica.MinCoreServerVersion && a.Name == manifest.Automatica.Name);

                foreach(var other in others)
                {
                    using (var newDbContext = new CoreContext(Config))
                    {
                        await PluginsController.DeletePlugin(newDbContext, GetCloudBlobContainer(), other);
                    }
                }
            }
        }

        [HttpPost, Route("deployPlugin/{apiKey}")]
        [DisableRequestSizeLimit]
        [NeedsRole(EF.Models.UserRole.SystemAdministrator)]
        public async Task DeployPlugin(Guid apiKey)
        {
            await DeployPluginAndDelete(apiKey, false);
        }

        [HttpPost, Route("deploy/{apiKey}")]
        [DisableRequestSizeLimit]
        [NeedsRole(EF.Models.UserRole.SystemAdministrator)]
        public async Task Deploy()
        {
            var container = GetCloudBlobContainer();
            var myFile = Request.Form.Files[0];

            var manifest = await UpdateHelper.GetUpdateManifest(myFile, Logger);

            if (manifest == null)
            {
                throw new ArgumentException("Invalid file..."); 
            }
            var version = DbContext.Versions.SingleOrDefault(a => a.VersionObj == manifest.Version && a.Rid == manifest.Rid);
            var isNewUpdate = false;

            if (version == null)
            {
                isNewUpdate = true;
                version = new ServerVersion()
                {
                    IsPrerelease = manifest.PreRelease,
                    Rid = manifest.Rid,
                    ChangeLog = "",
                    Version = manifest.Version.ToString()
                };
            }
            if (await UpdateHelper.UploadUpdateFile(Logger, myFile, container, version))
            {

                if (isNewUpdate)
                {
                    DbContext.Versions.Add(version);
                }
                else
                {
                    DbContext.Versions.Update(version);
                }
                DbContext.SaveChanges();
            }
        }

        [HttpGet, Route("plugins/{coreServerVersion}/{apiKey}")]
        [NeedsRole(EF.Models.UserRole.SystemAdministrator)]
        public IEnumerable<Plugin> GetAvailablePlugins(string coreServerVersion)
        {
            using (var dbContext = new CoreContext(Config))
            {
                var versionObj = new System.Version(coreServerVersion);
                var versions = dbContext.Plugins.Where(a => versionObj >= a.MinCoreServerVersionObj).ToList();

                return from r in versions
                                        group r by r.PluginGuid into g
                                        select g.OrderByDescending(x_ => x_.VersionObj).First();
            }
        }
    }
}
