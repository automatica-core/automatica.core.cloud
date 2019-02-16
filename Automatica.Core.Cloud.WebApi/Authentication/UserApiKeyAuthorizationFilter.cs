using Automatica.Core.Cloud.EF.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;

namespace Automatica.Core.Cloud.WebApi.Authentication
{
    public class UserApiKeyAuthorizationAttribute : TypeFilterAttribute
    {
        public UserApiKeyAuthorizationAttribute() : base(typeof(UserApiKeyAuthorizationFilter))
        {
            
        }

    }

    public class UserApiKeyAuthorizationFilter : IAuthorizationFilter
    {
        public UserApiKeyAuthorizationFilter(CoreContext context)
        {
            Context = context;
        }

        public CoreContext Context { get; }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var action = (Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)context.ActionDescriptor;
            var needsRoleAtts = action.MethodInfo.GetCustomAttributes(typeof(NeedsRoleAttribute), false);
            var needsRole = UserRole.Nothing;

            if(needsRoleAtts.Length > 0)
            {
                var needsRoleAttribute = (NeedsRoleAttribute)needsRoleAtts[0];
                needsRole = needsRoleAttribute.Role;
            }
           
            var uri = context.HttpContext.Request.Path.Value.Split("/", StringSplitOptions.RemoveEmptyEntries);
            var apiKey = uri[uri.Length - 1];
            try
            {
                var existing = Context.Users.SingleOrDefault(a => a.ApiKey == new Guid(apiKey));

                if(existing == null)
                {
                    context.Result = new ForbidResult();
                }

                if(existing.UserRole < needsRole)
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
