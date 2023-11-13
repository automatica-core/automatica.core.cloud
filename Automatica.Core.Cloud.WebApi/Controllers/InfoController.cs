using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    [Route("webapi/v{version:apiVersion}/info"), ApiVersion("1.0")]
    public class InfoController : BaseController
    {
        [HttpGet, Route("")]
        public string Info()
        {
            return "This is sparta";
        }

        public InfoController(IConfiguration config) : base(config)
        {
        }
    }
}
