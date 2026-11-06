using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Cloud.Storage;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Cloud.WebApi.Services
{
    /// <summary>Storage for plugin and update packages (blob container "automaticaupdates").</summary>
    public interface IPackageStore
    {
        Task<Uri> UploadAsync(string name, Stream content, IDictionary<string, string> metadata, CancellationToken token = default);
        Task DeleteAsync(string name, CancellationToken token = default);
    }

    public class AzurePackageStore : IPackageStore
    {
        private readonly IConfiguration _config;

        public AzurePackageStore(IConfiguration config)
        {
            _config = config;
        }

        public async Task<Uri> UploadAsync(string name, Stream content, IDictionary<string, string> metadata, CancellationToken token = default)
        {
            var container = BlobContainers.Create(_config, "AutomaticaCoreUpdateStore", "automaticaupdates");
            var blob = container.GetBlobClient(name);
            await blob.DeleteIfExistsAsync(cancellationToken: token);
            await blob.UploadAsync(content, new BlobUploadOptions
            {
                Metadata = metadata,
                HttpHeaders = new BlobHttpHeaders { ContentType = "application/zip" }
            }, token);
            return blob.Uri;
        }

        public async Task DeleteAsync(string name, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }
            var container = BlobContainers.Create(_config, "AutomaticaCoreUpdateStore", "automaticaupdates");
            await container.GetBlobClient(name).DeleteIfExistsAsync(cancellationToken: token);
        }
    }
}
