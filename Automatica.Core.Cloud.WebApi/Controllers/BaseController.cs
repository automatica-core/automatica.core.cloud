using Automatica.Core.Cloud.EF.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    public class BaseController : Controller
    {
        public BaseController(IConfiguration config)
        {
            Config = config;
        }

        public IConfiguration Config { get; }
        protected async Task<CoreServer> CheckIfServerExistsAndIsValid(CoreContext dbContext, Guid apiKey, Guid? serverId)
        {
            var server = dbContext.CoreServers.SingleOrDefault(a => a.ApiKey == apiKey);

            if (server == null)
            {
                throw new ArgumentException("Api key invalid");
            }

            if (serverId.HasValue)
            {
                if (server.ServerGuid != serverId.Value)
                {
                    throw new ArgumentException("Invalid apiKey/serverId match");
                }
            }

            return server;
        }

        [ApiExplorerSettings(IgnoreApi= true)]
        public Guid GetUserObjId()
        {
            return new Guid(User.Claims.SingleOrDefault(a => a.Type == "ObjId").Value);
        }
    }
}
