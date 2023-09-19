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
    [Route("webapi/v{version:apiVersion}/coreServerVersion"), ApiVersion("1.0")]
    [UserExists]
    [AuthorizeRole(Role = UserRole.SystemAdministrator)]
    public class CoreServerVersionsController : AzureStorageController
    {

        public CoreServerVersionsController(CoreContext dbContext, IConfiguration config, ILogger<CoreServerVersionsController> logger) : base(config)
        {
            DbContext = dbContext;
            Logger = logger;
        }

        public CoreContext DbContext { get; }
        public ILogger Logger { get; }

        [HttpGet]
        [Route("")]
        public IList<ServerVersion> GetVersions()
        {
            return DbContext.Versions.ToList();
        }

        [HttpPost, Route("{objId}/upload")]
        [DisableRequestSizeLimit]
        public async Task Upload(Guid objId)
        {
            var coreServerVersion = DbContext.Versions.SingleOrDefault(a => a.ObjId == objId);

            if(coreServerVersion == null)
            {
                throw new ArgumentException("ElementNotFound");
            }
            var container = GetCloudBlobContainer();
            var myFile = Request.Form.Files[0];

            await Update.UpdateHelper.UploadUpdateFile(Logger, myFile, container, coreServerVersion);

            DbContext.Versions.Update(coreServerVersion);
            await DbContext.SaveChangesAsync();
        }

        [HttpDelete, Route("delete")]
        public async Task Delete(Guid objId)
        {
            var coreServerVersion = DbContext.Versions.SingleOrDefault(a => a.ObjId == objId);

            if (coreServerVersion != null)
            {
                var container = GetCloudBlobContainer();
                var azureFileName = coreServerVersion.AzureFileName;
                if (!String.IsNullOrEmpty(azureFileName))
                {
                    CloudBlockBlob blob = container.GetBlockBlobReference(azureFileName);
                    await blob.DeleteIfExistsAsync();
                }
                DbContext.Remove(coreServerVersion);
                DbContext.SaveChanges();
            }
        }

        [HttpDelete, Route("deleteAllButLatest")]
        public Task DeleteAllButLatest()
        {
            var versions = DbContext.Versions.OrderByDescending(a => a.VersionObj).GroupBy(a => a.Rid);

            return Task.CompletedTask;
        }


        [HttpPost, Route("")]
        public ServerVersion Save([FromBody] ServerVersion serverVersion)
        {
            var coreServerVersion = DbContext.Versions.SingleOrDefault(a => a.ObjId == serverVersion.ObjId);

            if (coreServerVersion != null)
            {
                coreServerVersion.Version = serverVersion.Version;
                coreServerVersion.IsPreRelease = serverVersion.IsPreRelease;
                coreServerVersion.IsPublic = serverVersion.IsPublic;
                coreServerVersion.ChangeLog = serverVersion.ChangeLog;
                coreServerVersion.Rid = serverVersion.Rid;
                DbContext.Versions.Update(coreServerVersion);
            }
            else
            {
                DbContext.Versions.Add(serverVersion);
            }
            DbContext.SaveChanges();

            return serverVersion;
        }
    }
}
