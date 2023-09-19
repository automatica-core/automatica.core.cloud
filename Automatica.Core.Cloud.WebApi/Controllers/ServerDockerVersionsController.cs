using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.WebApi.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    [Route("webapi/v{version:apiVersion}/coreServerDockerVersion"), ApiVersion("1.0")]
    [UserExists]
    [AuthorizeRole(Role = UserRole.SystemAdministrator)]
    public class CoreServerDockerVersionsController : AzureStorageController
    {

        public CoreServerDockerVersionsController(CoreContext dbContext, IConfiguration config, ILogger<CoreServerVersionsController> logger) : base(config)
        {
            DbContext = dbContext;
            Logger = logger;
        }

        public CoreContext DbContext { get; }
        public ILogger Logger { get; }

        [HttpGet]
        [Route("")]
        public IList<ServerDockerVersion> GetVersions()
        {
            return DbContext.DockerVersions.ToList();
        }

        [HttpDelete, Route("delete")]
        public async Task Delete(Guid objId)
        {
            var coreServerVersion = DbContext.DockerVersions.SingleOrDefault(a => a.ObjId == objId);

            if (coreServerVersion != null)
            {
                DbContext.Remove(coreServerVersion);
                await DbContext.SaveChangesAsync();
            }
        }

        [HttpDelete, Route("deleteAllButLatest")]
        public Task DeleteAllButLatest()
        {
            var versions = DbContext.DockerVersions.OrderByDescending(a => a.VersionObj);

            return Task.CompletedTask;
        }


        [HttpPost, Route("")]
        public ServerDockerVersion Save([FromBody] ServerDockerVersion serverVersion)
        {
            var coreServerVersion = DbContext.DockerVersions.SingleOrDefault(a => a.ObjId == serverVersion.ObjId);

            if (coreServerVersion != null)
            {
                coreServerVersion.Version = serverVersion.Version;
                coreServerVersion.IsPreRelease = serverVersion.IsPreRelease;
                coreServerVersion.IsPublic = serverVersion.IsPublic;
                coreServerVersion.ChangeLog = serverVersion.ChangeLog;
                coreServerVersion.Branch = serverVersion.Branch;
                coreServerVersion.ImageName = serverVersion.ImageName;
                coreServerVersion.ImageTag = serverVersion.ImageTag;
                DbContext.DockerVersions.Update(coreServerVersion);
            }
            else
            {
                DbContext.DockerVersions.Add(serverVersion);
            }
            DbContext.SaveChanges();

            return serverVersion;
        }
    }
}
