using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Cloud.Storage
{
    public static class BlobContainers
    {
        /// <summary>
        /// Creates a container client from ConnectionStrings:{name}. The value is either a storage account
        /// endpoint ("https://account.blob.core.windows.net", authenticated with the managed identity) or,
        /// for local development, a classic connection string.
        /// </summary>
        public static BlobContainerClient Create(IConfiguration config, string connectionName, string containerName)
        {
            var value = config.GetConnectionString(connectionName);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"ConnectionStrings:{connectionName} is not configured");
            }

            if (Uri.TryCreate(value, UriKind.Absolute, out var endpoint) && endpoint.Scheme == Uri.UriSchemeHttps)
            {
                var service = new BlobServiceClient(endpoint, new DefaultAzureCredential());
                return service.GetBlobContainerClient(containerName);
            }

            return new BlobContainerClient(value, containerName);
        }
    }
}
