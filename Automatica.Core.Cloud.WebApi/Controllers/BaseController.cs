using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    public class BaseController : Controller
    {
        [ApiExplorerSettings(IgnoreApi= true)]
        public Guid GetUserObjId()
        {
            return new Guid(User.Claims.SingleOrDefault(a => a.Type == "ObjId").Value);
        }
    }
}
