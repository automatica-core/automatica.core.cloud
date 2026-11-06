using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    [AllowAnonymous, Route("webapi/v{version:apiVersion}/info"), ApiVersion("1.0")]
    public class InfoController : BaseController
    {
        [HttpGet, Route("")]
        public string Info()
        {
            return "automatica cloud";
        }
    }
}
