using Automatica.Core.Cloud.EF.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Automatica.Core.Cloud.WebApi.Authentication
{
    public class ServerApiKeyAuthorizationAttribute : TypeFilterAttribute
    {
        public ServerApiKeyAuthorizationAttribute() : base(typeof(ServerApiKeyAuthorizationFilter))
        {

        }
    }

    public class ServerApiKeyAuthorizationFilter : IAuthorizationFilter
    {
        public ServerApiKeyAuthorizationFilter(CoreContext context)
        {
            Context = context;
        }

        public CoreContext Context { get; }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var uri = context.HttpContext.Request.Path.Value.Split("/", StringSplitOptions.RemoveEmptyEntries);
            var apiKey = uri[uri.Length - 1];
            try
            {
                var existing = Context.CoreServers.SingleOrDefault(a => a.ApiKey == new Guid(apiKey));

                if(existing == null)
                {
                    context.Result = new ForbidResult();
                }
            }
            catch
            {
                context.Result = new ForbidResult();
            }


        }
    }
}
