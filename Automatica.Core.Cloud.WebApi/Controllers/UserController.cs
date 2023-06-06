using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.WebApi.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    public class UserAuthData
    {
        [JsonProperty(PropertyName = "username")]
        public string UserName { get; set; }

        [JsonProperty(PropertyName = "password")]
        public string Password { get; set; }
    }

    [Route("webapi/v{version:apiVersion}/user"), ApiVersion("1.0")]
    [AllowAnonymous]
    public class UserController : BaseController
    {
        private readonly CoreContext _context;
        private readonly JwtInfoProvider _info;

        public UserController(CoreContext context, JwtInfoProvider info)
        {
            _context = context;
            _info = info;
        }

        [HttpPost]
        [Route("login")]
        public async Task<User> Login([FromBody]UserAuthData data)
        {
            var user = await _context.Users.SingleOrDefaultAsync(a => a.UserName == data.UserName);

            if (user == null)
            {
                return null;
            }

            var salt = user.Salt;

            var hash = EF.Models.User.HashPassword(data.Password, salt);

            if (hash == user.PasswordHash)
            {
                user = LoginUser(user);
                user.Salt = null;
                user.PasswordHash = "";
                return user;
            }

            return null;
        }

        //[HttpPost]
        //[Route("create")]
        //public async Task<string> CreateUser([FromBody]User user)
        //{
        //    var dbUser = await _context.Users.SingleOrDefaultAsync(a => a.UserName == user.UserName);

        //    if (dbUser != null)
        //    {
        //        return "COMMON.LOGIN.USERNAME_EXISTS";
        //    }


        //    dbUser = await _context.Users.SingleOrDefaultAsync(a => a.Email == user.Email);

        //    if (dbUser != null)
        //    {
        //        return "COMMON.LOGIN.EMAIL_EXISTS";
        //    }

        //    user.Salt = User.GenerateNewSalt();
        //    user.ObjId = Guid.NewGuid();
        //    user.Enabled = false;
        //    user.ActivationCode = Guid.NewGuid().ToString();

        //    _context.Users.Add(user);
        //    _context.SaveChanges();

        //    return "SUCCESS";
        //}

        private User LoginUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName),
                new Claim("ObjId", user.ObjId.ToString()),
                new Claim(ClaimTypes.Role, user.UserRole.ToString())
            };

            //foreach (var role in user.InverseThis2Roles)
            //{
            //    claims.Add(new Claim(ClaimTypes.Role, role.This2RoleNavigation.Key));
            //}

            //foreach (var userGroups in DbContext.User2Groups.Where(a => a.This2User == user.ObjId))
            //{
            //    var roles = DbContext.UserGroup2Roles.Where(a => a.This2UserGroup == userGroups.This2UserGroup).Include(a => a.This2RoleNavigation);

            //    foreach (var userGroupRoles in roles)
            //    {
            //        claims.Add(new Claim(ClaimTypes.Role, userGroupRoles.This2RoleNavigation.Key));

            //        user.InverseThis2Roles.Add(new User2Role()
            //        {
            //            This2User = user.ObjId,
            //            This2Role = userGroupRoles.This2Role,
            //            This2RoleNavigation = userGroupRoles.This2RoleNavigation
            //        });
            //    }

            //    claims.Add(new Claim(UserGroup.ClaimType, userGroups.This2UserGroup.ToString()));
            //}

            var claimsIdentity = new ClaimsIdentity(claims);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = _info.Key;
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claimsIdentity,
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(token);


            return user;
        }
    }
}
