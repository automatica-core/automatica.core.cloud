using Automatica.Core.Cloud.EF.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;

namespace Automatica.Core.Cloud.WebApi.Authentication
{
    public class UserExistsAttribute : TypeFilterAttribute
    {
        public UserExistsAttribute() : base(typeof(UserExistsFilter))
        {
       
        }
    }

    public class UserExistsFilter : IAuthorizationFilter
    {
        public UserExistsFilter(CoreContext context)
        {
            Context = context;
        }

        public CoreContext Context { get; }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var claim = context.HttpContext.User.Claims.SingleOrDefault(a => a.Type == "ObjId");

            if(claim == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            var user = Context.Users.SingleOrDefault(a => a.ObjId == new Guid(claim.Value));

            if(user == null)
            {
                context.Result = new ForbidResult();
            }

        }
    }
}
