using System;
using Automatica.Core.Cloud.WebApi.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        protected Guid CurrentUserId => User.GetUserId();
        protected bool IsAdmin => User.IsAdmin();
        protected bool IsSysAdmin => User.IsSysAdmin();
        protected string ClientIp => RateLimits.ClientIp(HttpContext);
    }
}
