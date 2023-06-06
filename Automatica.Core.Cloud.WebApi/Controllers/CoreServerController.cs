using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.WebApi.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    [Route("webapi/v{version:apiVersion}/coreServer"), ApiVersion("1.0")]
    [UserExists]
    public class CoreServerController : AzureStorageController
    {
        public CoreServerController(CoreContext dbContext, IConfiguration config) : base(config)
        {
            DbContext = dbContext;
        }

        public CoreContext DbContext { get; }

        [HttpDelete, Route("delete")]
        public void Delete(Guid objId)
        {
            var coreServer = DbContext.CoreServers.SingleOrDefault(a => a.ObjId == objId);

            if (coreServer != null)
            {
                DbContext.Remove(coreServer);
                DbContext.SaveChanges();
            }
        }


        [HttpPost, Route("")]
        public CoreServer Save([FromBody]CoreServer newObj)
        {
            var coreServer = DbContext.CoreServers.SingleOrDefault(a => a.ObjId == newObj.ObjId);

            if (coreServer != null)
            {
                DbContext.Entry(coreServer).State = EntityState.Detached;
                DbContext.CoreServers.Update(newObj);
            }
            else
            {
                newObj.This2User = GetUserObjId();
                newObj.ApiKey = Guid.NewGuid();
                DbContext.CoreServers.Add(newObj);
            }
            DbContext.SaveChanges();

            return newObj;
        }


        [HttpGet]
        [Route("")]
        public IList<CoreServer> GetServers()
        {
            return DbContext.CoreServers.ToList();
        }
    }
}
