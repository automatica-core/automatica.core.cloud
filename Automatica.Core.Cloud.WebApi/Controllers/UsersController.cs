using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.WebApi.Authentication;
using Automatica.Core.Cloud.WebApi.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    [ApiController]
    [Route("webapi/v{version:apiVersion}/users"), ApiVersion("1.0")]
    [Authorize(Policy = Policies.SysAdmin)]
    public class UsersController : BaseController
    {
        private readonly CoreContext _db;
        private readonly TokenService _tokens;

        public UsersController(CoreContext db, TokenService tokens)
        {
            _db = db;
            _tokens = tokens;
        }

        internal static string ValidatePassword(string password, User user)
        {
            if (string.IsNullOrEmpty(password) || password.Length < PasswordHashing.MinPasswordLength)
            {
                return "password-too-short";
            }
            if (user != null && (string.Equals(password, user.UserName, StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(password, user.Email, StringComparison.OrdinalIgnoreCase)))
            {
                return "password-equals-username";
            }
            return null;
        }

        [HttpGet("")]
        public async Task<IList<UserDto>> GetUsers()
        {
            var users = await _db.Users.AsNoTracking().OrderBy(a => a.UserName).ToListAsync();
            return users.Select(UserDto.From).ToList();
        }

        [HttpPost("")]
        public async Task<ActionResult<UserCreatedDto>> Create([FromBody] UserSaveDto data)
        {
            if (await _db.Users.AnyAsync(a => a.UserName == data.UserName))
            {
                return Conflict(new { error = "username-exists" });
            }
            if (!string.IsNullOrEmpty(data.Email) && await _db.Users.AnyAsync(a => a.Email == data.Email))
            {
                return Conflict(new { error = "email-exists" });
            }

            var user = new User
            {
                ObjId = Guid.NewGuid(),
                UserName = data.UserName,
                Email = data.Email,
                FirstName = data.FirstName ?? "",
                LastName = data.LastName ?? "",
                PublisherName = data.PublisherName,
                UserRole = data.Role,
                Enabled = data.Enabled,
                ApiKey = Guid.NewGuid(),
                Salt = PasswordHashing.GenerateSalt()
            };

            string generated = null;
            var password = data.Password;
            if (string.IsNullOrEmpty(password))
            {
                password = generated = PasswordHashing.GeneratePassword();
            }
            var error = ValidatePassword(password, user);
            if (error != null)
            {
                return BadRequest(new { error });
            }

            user.PasswordHash = PasswordHashing.Hash(password, user.Salt);
            user.RenewSecurityStamp();
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return new UserCreatedDto { User = UserDto.From(user), InitialPassword = generated };
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<UserDto>> Update(Guid id, [FromBody] UserSaveDto data)
        {
            var user = await _db.Users.SingleOrDefaultAsync(a => a.ObjId == id);
            if (user == null)
            {
                return NotFound();
            }

            if (id == CurrentUserId && (data.Role != user.UserRole || !data.Enabled))
            {
                return BadRequest(new { error = "cannot-demote-self" });
            }
            if (data.UserName != user.UserName && await _db.Users.AnyAsync(a => a.UserName == data.UserName && a.ObjId != id))
            {
                return Conflict(new { error = "username-exists" });
            }

            var securityRelevant = user.UserRole != data.Role || user.Enabled != data.Enabled;

            user.UserName = data.UserName;
            user.Email = data.Email;
            user.FirstName = data.FirstName ?? "";
            user.LastName = data.LastName ?? "";
            user.PublisherName = data.PublisherName;
            user.UserRole = data.Role;
            user.Enabled = data.Enabled;
            if (securityRelevant)
            {
                user.RenewSecurityStamp();
            }
            await _db.SaveChangesAsync();
            if (securityRelevant)
            {
                await _tokens.RevokeAllForUserAsync(user.ObjId);
            }
            return UserDto.From(user);
        }

        /// <summary>Sets a new password (generated when empty); signs the user out everywhere.</summary>
        [HttpPost("{id:guid}/password")]
        public async Task<ActionResult<UserCreatedDto>> ResetPassword(Guid id, [FromBody] PasswordDto data)
        {
            var user = await _db.Users.SingleOrDefaultAsync(a => a.ObjId == id);
            if (user == null)
            {
                return NotFound();
            }

            string generated = null;
            var password = data?.NewPassword;
            if (string.IsNullOrEmpty(password))
            {
                password = generated = PasswordHashing.GeneratePassword();
            }
            var error = ValidatePassword(password, user);
            if (error != null)
            {
                return BadRequest(new { error });
            }

            user.Salt = PasswordHashing.GenerateSalt();
            user.PasswordHash = PasswordHashing.Hash(password, user.Salt);
            user.FailedLoginCount = 0;
            user.LockedUntil = null;
            user.RenewSecurityStamp();
            await _db.SaveChangesAsync();
            await _tokens.RevokeAllForUserAsync(user.ObjId);

            return new UserCreatedDto { User = UserDto.From(user), InitialPassword = generated };
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == CurrentUserId)
            {
                return BadRequest(new { error = "cannot-delete-self" });
            }
            var user = await _db.Users.SingleOrDefaultAsync(a => a.ObjId == id);
            if (user == null)
            {
                return NotFound();
            }
            if (await _db.CoreServers.AnyAsync(a => a.This2User == id) || await _db.Plugins.AnyAsync(a => a.This2User == id) ||
                await _db.DeployKeys.AnyAsync(a => a.This2User == id))
            {
                return Conflict(new { error = "user-has-servers-plugins-or-keys" });
            }

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
