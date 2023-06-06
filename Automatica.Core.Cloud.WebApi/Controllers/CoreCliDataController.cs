using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.WebApi.Authentication;
using Automatica.Core.Cloud.WebApi.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    [Route("webapi/v{version:apiVersion}/coreCliData"), ApiVersion("1.0")]
    [AllowAnonymous]
    [UserApiKeyAuthorization] // API Key must be at the last position!!
    [NeedsRole(UserRole.SystemAdministrator)]
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
        

        [HttpPost, Route("deployPlugin/{deleteOldPackages}/{branch}/{apiKey}")]
        [DisableRequestSizeLimit]
        [NeedsRole(UserRole.SystemAdministrator)]
        public async Task DeployPluginAndDelete(Guid apiKey, bool deleteOldPackages, string branch)
        {
            var container = GetCloudBlobContainer();
            var myFile = Request.Form.Files[0];

            var manifest = await PluginHelper.UploadAndSave(DbContext, Logger, myFile, container, apiKey, branch);

            if(deleteOldPackages)
            {
                // delete only other plugins with the same min core server version to stay compatible
                var others = DbContext.Plugins.Where(a => a.VersionObj < manifest.Automatica.PluginVersion && a.MinCoreServerVersionObj == manifest.Automatica.MinCoreServerVersion && a.Name == manifest.Automatica.Name && a.Branch == branch);

                foreach(var other in others)
                {
                    using var newDbContext = new CoreContext(Config);
                    await PluginsController.DeletePlugin(newDbContext, GetCloudBlobContainer(), other);
                }
            }
        }

        [HttpPost, Route("deployPlugin/{deleteOldPackages}/{apiKey}")]
        [DisableRequestSizeLimit]
        [NeedsRole(UserRole.SystemAdministrator)]
        public async Task DeployPluginAndDeleteWithoutSpecificBranch(Guid apiKey, bool deleteOldPackages)
        {
            await DeployPluginAndDelete(apiKey, deleteOldPackages, "develop");
        }

        [HttpPost, Route("deployPlugin/{apiKey}")]
        [DisableRequestSizeLimit]
        [NeedsRole(UserRole.SystemAdministrator)]
        public async Task DeployPlugin(Guid apiKey)
        {
            await DeployPluginAndDelete(apiKey, false, "develop");
        }

        [HttpPost, Route("deploy/{branch}/{apiKey}")]
        [DisableRequestSizeLimit]
        [NeedsRole(UserRole.SystemAdministrator)]
        public async Task Deploy(string branch)
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
                    Version = manifest.Version.ToString(),
                    Branch = branch
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

        [HttpPost, Route("deploy/{apiKey}")]
        [DisableRequestSizeLimit]
        [NeedsRole(UserRole.SystemAdministrator)]
        public Task Deploy()
        {
            return Deploy("develop");
        }

        [HttpGet, Route("plugins/{coreServerVersion}/{apiKey}")]
        [NeedsRole(UserRole.SystemAdministrator)]
        public IEnumerable<Plugin> GetAvailablePlugins(string coreServerVersion)
        {
            return GetAvailablePlugins(coreServerVersion, "develop");
        }

        [HttpGet, Route("plugins/{coreServerVersion}/{branch}/{apiKey}")]
        [NeedsRole(UserRole.SystemAdministrator)]
        public IEnumerable<Plugin> GetAvailablePlugins(string coreServerVersion, string branch)
        {
            using var dbContext = new CoreContext(Config);
            var versionObj = new Version(coreServerVersion);
            var versions = dbContext.Plugins.Where(a => versionObj >= a.MinCoreServerVersionObj && a.Branch == branch).ToList();

            return from r in versions
                group r by r.PluginGuid into g
                select g.OrderByDescending(x => x.VersionObj).First();
        }
    }
}
