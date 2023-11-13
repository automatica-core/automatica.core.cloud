using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    public class AzureStorageController : BaseController
    {
        public AzureStorageController(IConfiguration config) : base(config)
        {
        }

        protected CloudBlobContainer GetCloudBlobContainer()
        {
            var storageAccount = CloudStorageAccount.Parse(Config.GetConnectionString("AutomaticaCoreUpdateStore"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("automaticaupdates");
            return container;
        }

    }
}
