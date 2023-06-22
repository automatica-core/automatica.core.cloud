using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Common.Update;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Plugin = Automatica.Core.Cloud.EF.Models.Plugin;

namespace Automatica.Core.Cloud.WebApi.Update
{
    public class PluginHelper
    {
        public static async Task<PluginManifest> GetPluginManifest(IFormFile file, ILogger logger)
        {
            var targetLocation = Path.GetTempPath();
            var path = Path.Combine(targetLocation, Path.GetFileName(file.FileName));

            if (File.Exists(path))
            {
                File.Delete(path);
            }
            using (var fileStream = File.Create(path))
            {
                await file.CopyToAsync(fileStream);
            }

            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString().Replace("-", ""));
            var ret = Common.Update.Plugin.GetPluginManifest(logger, path, tempPath);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }
            return ret;
        }

        public static async Task<PluginManifest> UploadAndSave(CoreContext dbContext, ILogger logger, IFormFile myFile, CloudBlobContainer container, Guid apiKey, string branch)
        {
            var manifest = await GetPluginManifest(myFile, logger);

            if (manifest == null)
            {
                throw new ArgumentException("Invalid file...");
            }
            var user = dbContext.Users.SingleOrDefault(a => a.ApiKey == apiKey);

            if (user == null)
            {
                throw new ArgumentException("User not found..");
            }

            var version = dbContext.Plugins.ToList().SingleOrDefault(a => a.VersionObj == manifest.Automatica.PluginVersion && a.Name == manifest.Automatica.Name);
            var isNewUpdate = false;

            if (version == null)
            {
                isNewUpdate = true;
                version = new Plugin
                {
                    Branch = branch,
                    IsPublic = false,
                    Name = manifest.Automatica.Name,
                    Version = manifest.Automatica.PluginVersion.ToString(),
                    MinCoreServerVersion = manifest.Automatica.MinCoreServerVersion.ToString(),
                    PluginType = manifest.Automatica.Type == "driver" ? PluginType.Driver : PluginType.Rule,
                    This2User = user.ObjId,
                    Publisher = user.PublisherName,
                    ComponentName = manifest.Automatica.ComponentName,
                    PluginGuid = manifest.Automatica.PluginGuid,
                    PluginVersion = manifest.Automatica.PluginVersion.ToString()
                    
                };

                foreach (var mandatory in manifest.Automatica.MandatoryLicenseFeatures)
                {
                    var feature = new PluginFeature();
                    feature.FeatureName = mandatory;
                    feature.IsMandatory = true;
                    feature.This2PluginNavigation = version;

                    version.LicenseFeatures.Add(feature);
                }

                foreach (var mandatory in manifest.Automatica.OptionalLicenseFeatures)
                {
                    var feature = new PluginFeature();
                    feature.FeatureName = mandatory;
                    feature.IsMandatory = false;
                    feature.This2PluginNavigation = version;

                    version.LicenseFeatures.Add(feature);
                }
            }
            else
            {
                version.PluginVersion = manifest.Automatica.PluginVersion.ToString();
                version.Version = manifest.Automatica.PluginVersion.ToString();
                version.MinCoreServerVersion = manifest.Automatica.MinCoreServerVersion.ToString();
            }
            if (await UploadPluginFile(logger, myFile, container, version, manifest.Automatica.PluginVersion, branch))
            {
                if (isNewUpdate)
                {
                    dbContext.Plugins.Add(version);
                }
                else
                {
                    dbContext.Plugins.Update(version);
                }
                dbContext.SaveChanges();
            }
            return manifest;
        }

        public static async Task<bool> UploadPluginFile(ILogger logger, IFormFile myFile, CloudBlobContainer container, Plugin plugin, Version packageVersion, string branch)
        {
            var targetLocation = Path.GetTempPath();

            var fileName = Path.GetFileName(myFile.FileName);
            var azureFileName = $"{packageVersion}-{branch}-{fileName}";
            CloudBlockBlob blob = container.GetBlockBlobReference(azureFileName);
            await blob.DeleteIfExistsAsync();

            var path = Path.Combine(targetLocation, fileName);

            if (File.Exists(path))
            {
                File.Delete(path);
            }
            using (var fileStream = File.Create(path))
            {
                myFile.CopyTo(fileStream);
            }

            if (!Common.Update.Plugin.CheckPluginFile(logger, path, false))
            {
                return false;
            }

            using (var fileStream = File.OpenRead(path))
            {
                await blob.UploadFromStreamAsync(fileStream);
            }

            blob.Metadata.Add("version", packageVersion.ToString());
            blob.Metadata.Add("branch", branch);
            await blob.SetMetadataAsync();

            plugin.AzureUrl = blob.Uri.ToString();
            plugin.AzureFileName = azureFileName;

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return true;
        }
    }
}
