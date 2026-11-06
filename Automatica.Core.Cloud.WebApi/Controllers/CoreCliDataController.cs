using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.WebApi.Authentication;
using Automatica.Core.Cloud.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    /// <summary>
    /// automatica-cli (CI) endpoints. Same routes as before, but the trailing {apiKey} segment (or the
    /// X-Automatica-Deploy-Key header) must be a scoped deploy key; user api keys no longer work.
    /// </summary>
    [AllowAnonymous]
    [Route("webapi/v{version:apiVersion}/coreCliData"), ApiVersion("1.0")]
    [DeployKey]
    [EnableRateLimiting(RateLimits.Cli)]
    [DisableRequestSizeLimit]
    public class CoreCliDataController : BaseController
    {
        private readonly CoreContext _db;
        private readonly PackageService _packages;

        public CoreCliDataController(CoreContext db, PackageService packages)
        {
            _db = db;
            _packages = packages;
        }

        private async Task<User> PublisherAsync()
        {
            var key = HttpContext.GetDeployKey();
            return await _db.Users.SingleAsync(a => a.ObjId == key.This2User);
        }

        [HttpPost, Route("deployPlugin/{deleteOldPackages}/{branch}/{apiKey}")]
        [RequireDeployScope(DeployKeyScope.DeployPlugin)]
        [RequestFormLimits(MultipartBodyLengthLimit = 1L << 30)]
        public async Task DeployPluginAndDelete(bool deleteOldPackages, string branch)
        {
            var file = Request.Form.Files[0];
            await _packages.DeployPluginAsync(file, await PublisherAsync(), branch, deleteOldPackages, HttpContext.RequestAborted);
        }

        [HttpPost, Route("deployPlugin/{deleteOldPackages}/{apiKey}")]
        [RequireDeployScope(DeployKeyScope.DeployPlugin)]
        [RequestFormLimits(MultipartBodyLengthLimit = 1L << 30)]
        public Task DeployPluginAndDeleteWithoutSpecificBranch(bool deleteOldPackages)
        {
            return DeployPluginAndDelete(deleteOldPackages, "develop");
        }

        [HttpPost, Route("deployPlugin/{apiKey}")]
        [RequireDeployScope(DeployKeyScope.DeployPlugin)]
        [RequestFormLimits(MultipartBodyLengthLimit = 1L << 30)]
        public Task DeployPlugin()
        {
            return DeployPluginAndDelete(false, "develop");
        }

        [HttpPost, Route("deploy/{branch}/{apiKey}")]
        [RequireDeployScope(DeployKeyScope.DeployUpdate)]
        [RequestFormLimits(MultipartBodyLengthLimit = 1L << 30)]
        public async Task Deploy(string branch)
        {
            var file = Request.Form.Files[0];
            await _packages.DeployUpdateAsync(file, branch, HttpContext.RequestAborted);
        }

        [HttpPost, Route("deploy/{apiKey}")]
        [RequireDeployScope(DeployKeyScope.DeployUpdate)]
        [RequestFormLimits(MultipartBodyLengthLimit = 1L << 30)]
        public Task Deploy()
        {
            return Deploy("develop");
        }

        [HttpPost, Route("deployDocker/{branch}/{apiKey}")]
        [RequireDeployScope(DeployKeyScope.DeployDocker)]
        public async Task<ServerDockerVersion> DeployDocker([FromBody] ServerDockerVersion serverDockerVersion, [FromRoute] string branch)
        {
            if (!PackageService.IsValidBranch(branch) || serverDockerVersion == null || !Version.TryParse(serverDockerVersion.Version, out _))
            {
                throw new ArgumentException("Invalid docker version");
            }

            var existing = await _db.DockerVersions.SingleOrDefaultAsync(a => a.ObjId == serverDockerVersion.ObjId);
            if (existing != null)
            {
                existing.Version = serverDockerVersion.Version;
                existing.IsPreRelease = serverDockerVersion.IsPreRelease;
                existing.IsPublic = serverDockerVersion.IsPublic;
                existing.ChangeLog = serverDockerVersion.ChangeLog;
                existing.Branch = branch;
                existing.ImageName = serverDockerVersion.ImageName;
                existing.ImageTag = serverDockerVersion.ImageTag;
            }
            else
            {
                serverDockerVersion.Branch = branch;
                _db.DockerVersions.Add(serverDockerVersion);
            }
            await _db.SaveChangesAsync();

            return serverDockerVersion;
        }

        [HttpGet, Route("plugins/{coreServerVersion}/{apiKey}")]
        [RequireDeployScope(DeployKeyScope.ReadPlugins)]
        public IEnumerable<Plugin> GetAvailablePlugins(string coreServerVersion)
        {
            return GetAvailablePlugins(coreServerVersion, "develop");
        }

        [HttpGet, Route("plugins/{coreServerVersion}/{branch}/{apiKey}")]
        [RequireDeployScope(DeployKeyScope.ReadPlugins)]
        public IEnumerable<Plugin> GetAvailablePlugins(string coreServerVersion, string branch)
        {
            var versionObj = new Version(coreServerVersion);
            var versions = _db.Plugins.AsNoTracking().Where(a => a.Branch == branch).ToList()
                .Where(a => a.MinCoreServerVersionObj != null && versionObj >= a.MinCoreServerVersionObj);

            return from r in versions
                   group r by r.PluginGuid into g
                   select g.OrderByDescending(x => x.VersionObj).First();
        }
    }
}
