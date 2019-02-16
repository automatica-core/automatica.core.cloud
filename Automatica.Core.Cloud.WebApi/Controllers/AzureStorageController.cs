using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Text;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    public class AzureStorageController : BaseController
    {
        public AzureStorageController(IConfiguration config)
        {
            Config = config;
        }

        public IConfiguration Config { get; }

        protected CloudBlobContainer GetCloudBlobContainer()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Config.GetConnectionString("AutomaticaCoreUpdateStore"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("automaticaupdates");
            return container;
        }

    }
}
