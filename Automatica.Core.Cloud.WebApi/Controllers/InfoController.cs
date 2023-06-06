using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
