using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.WebApi.Authentication;
using Automatica.Core.Cloud.WebApi.Update;
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
    [Route("v{version:apiVersion}/plugins"), ApiVersion("1.0")]
    [UserExists]
    [AuthorizeRole(Role = UserRole.SystemAdministrator)]
    public class PluginsController : AzureStorageController
    {

        public PluginsController(CoreContext dbContext, IConfiguration config, ILogger<PluginsController> logger) : base(config)
        {
            DbContext = dbContext;
            Logger = logger;
        }

        public CoreContext DbContext { get; }
        public ILogger<PluginsController> Logger { get; }

        [HttpGet]
        [Route("")]
        public IList<Plugin> GetVersions()
        {
            return DbContext.Plugins.ToList();
        }

        [HttpPost, Route("upload")]
        [DisableRequestSizeLimit]
        public async Task Upload()
        {
            var container = GetCloudBlobContainer();
            var myFile = Request.Form.Files[0];

            var x = User.FindFirst("ObjId");
            var user = DbContext.Users.SingleOrDefault(a => a.ObjId == new Guid(x.Value));

            if(user == null)
            {
                throw new ArgumentException("Invalid user");
            }
            await PluginHelper.UploadAndSave(DbContext, Logger, myFile, container, user.ApiKey, "develop");
        }

        [HttpPost, Route("{objId}/upload")]
        [DisableRequestSizeLimit]
        public async Task Upload(Guid objId)
        {
            var plugin = DbContext.Plugins.SingleOrDefault(a => a.ObjId == objId);

            if (plugin == null)
            {
                throw new ArgumentException("ElementNotFound");
            }
            var container = GetCloudBlobContainer();
            var myFile = Request.Form.Files[0];

            if(await PluginHelper.UploadPluginFile(Logger, myFile, container, plugin, plugin.VersionObj, "develop"))
            {
                var manifest = await PluginHelper.GetPluginManifest(myFile, Logger);
                plugin.Name = manifest.Automatica.Name;
                plugin.Version = manifest.Automatica.PluginVersion.ToString();
                plugin.MinCoreServerVersion = manifest.Automatica.MinCoreServerVersion.ToString();
                plugin.PluginType = manifest.Automatica.Type == "driver" ? PluginType.Driver : PluginType.Rule;
                plugin.ComponentName = manifest.Automatica.ComponentName;

                DbContext.Plugins.Update(plugin);
                DbContext.SaveChanges();
            }

        }

        [HttpDelete, Route("delete")]
        public async Task Delete(Guid objId)
        {
            var plugin = DbContext.Plugins.SingleOrDefault(a => a.ObjId == objId);

            await DeletePlugin(DbContext, GetCloudBlobContainer(), plugin);
        }

        internal static async Task DeletePlugin(CoreContext dbContext, CloudBlobContainer container, Plugin plugin)
        {
            if (plugin != null)
            {
                var azureFileName = plugin.AzureFileName;
                if (!String.IsNullOrEmpty(azureFileName))
                {
                    CloudBlockBlob blob = container.GetBlockBlobReference(azureFileName);
                    await blob.DeleteIfExistsAsync();
                }
                dbContext.Remove(plugin);
                dbContext.SaveChanges();
            }
        }


        [HttpPost, Route("")]
        public Plugin Save([FromBody] Plugin plugin)
        {
            var dbPlugin = DbContext.Plugins.SingleOrDefault(a => a.ObjId == plugin.ObjId);

            if (dbPlugin != null)
            {
                dbPlugin.Version = plugin.Version;
                dbPlugin.IsPrerelease = plugin.IsPrerelease;
                dbPlugin.IsPublic = plugin.IsPublic;
                dbPlugin.Publisher = plugin.Publisher;
                dbPlugin.MinCoreServerVersion = plugin.MinCoreServerVersion;
                DbContext.Plugins.Update(dbPlugin);
            }
            else
            {
                plugin.This2User = DbContext.Users.First(a => a.UserName == "sa").ObjId;
                DbContext.Plugins.Add(plugin);
            }
            DbContext.SaveChanges();

            return plugin;
        }
    }
}
