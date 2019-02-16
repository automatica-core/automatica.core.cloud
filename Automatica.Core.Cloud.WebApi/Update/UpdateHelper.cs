using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Common.Update;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Automatica.Core.Cloud.WebApi.Update
{
    public class UpdateHelper
    {
        public static async Task<UpdateManifest> GetUpdateManifest(IFormFile file, ILogger logger)
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
            var ret = Common.Update.Update.GetUpdateManifest(logger, path, tempPath);

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            if(Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }
            return ret;
        }

        public static async Task<bool> UploadUpdateFile(ILogger logger, IFormFile myFile, CloudBlobContainer container, ServerVersion coreServerVersion)
        {
            var targetLocation = Path.GetTempPath();

            var fileName = Path.GetFileName(myFile.FileName);
            var azureFileName = $"{coreServerVersion.Rid}_{coreServerVersion.Version}-{fileName}";
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

            if (!Common.Update.Update.CheckUpdateFile(logger, path, coreServerVersion.Rid))
            {
                return false;
            }

            using (var fileStream = File.OpenRead(path))
            {
                await blob.UploadFromStreamAsync(fileStream);
            }

            blob.Metadata.Add("version", coreServerVersion.Version);
            blob.Metadata.Add("rid", coreServerVersion.Rid);
            await blob.SetMetadataAsync();

            coreServerVersion.AzureUrl = blob.Uri.ToString();
            coreServerVersion.AzureFileName = azureFileName;


            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return true;
        }
    }
}
