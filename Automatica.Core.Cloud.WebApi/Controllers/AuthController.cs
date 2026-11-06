using System;
using System.Threading.Tasks;
using Asp.Versioning;
using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.WebApi.Authentication;
using Automatica.Core.Cloud.WebApi.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    [ApiController]
    [Route("webapi/v{version:apiVersion}/auth"), ApiVersion("1.0")]
    public class AuthController : BaseController
    {
        /// <summary>Refresh and logout must carry this header; together with SameSite=Strict it stops CSRF.</summary>
        public const string CsrfHeader = "X-Requested-With";
        public const string CsrfValue = "automatica-cloud";

        private const int MaxFailedLogins = 8;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly CoreContext _db;
        private readonly TokenService _tokens;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<AuthController> _logger;

        public AuthController(CoreContext db, TokenService tokens, IHostEnvironment environment, ILogger<AuthController> logger)
        {
            _db = db;
            _tokens = tokens;
            _environment = environment;
            _logger = logger;
        }

        [HttpPost("login"), AllowAnonymous, EnableRateLimiting(RateLimits.Login)]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto data)
        {
            var user = await _db.Users.FirstOrDefaultAsync(a => a.UserName == data.UserName)
                       ?? await _db.Users.FirstOrDefaultAsync(a => a.Email == data.UserName);

            if (user == null)
            {
                PasswordHashing.DummyVerify(data.Password);
                return Unauthorized();
            }

            if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
            {
                PasswordHashing.DummyVerify(data.Password);
                return Unauthorized();
            }

            if (!PasswordHashing.Verify(data.Password, user.Salt, user.PasswordHash))
            {
                user.FailedLoginCount++;
                if (user.FailedLoginCount >= MaxFailedLogins)
                {
                    user.LockedUntil = DateTime.UtcNow.Add(LockoutDuration);
                    user.FailedLoginCount = 0;
                    _logger.LogWarning("Locked user {UserId} after repeated failed logins", user.ObjId);
                }
                await _db.SaveChangesAsync();
                return Unauthorized();
            }

            if (!user.Enabled)
            {
                return Unauthorized();
            }

            if (PasswordHashing.IsLegacy(user.PasswordHash))
            {
                user.Salt = PasswordHashing.GenerateSalt();
                user.PasswordHash = PasswordHashing.Hash(data.Password, user.Salt);
            }
            if (string.IsNullOrEmpty(user.SecurityStamp))
            {
                user.RenewSecurityStamp();
            }
            user.FailedLoginCount = 0;
            user.LockedUntil = null;
            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var issued = await _tokens.IssueAsync(user, ClientIp);
            SetRefreshCookie(issued.RefreshToken, issued.RefreshTokenExpiresAt);
            return new AuthResponseDto { AccessToken = issued.AccessToken, ExpiresAt = issued.AccessTokenExpiresAt, User = UserDto.From(user) };
        }

        [HttpPost("refresh"), AllowAnonymous, EnableRateLimiting(RateLimits.Refresh)]
        public async Task<ActionResult<AuthResponseDto>> Refresh()
        {
            if (Request.Headers[CsrfHeader] != CsrfValue)
            {
                return BadRequest();
            }

            var raw = Request.Cookies[TokenService.RefreshCookieName];
            var result = await _tokens.RefreshAsync(raw, ClientIp);
            if (result == null)
            {
                ClearRefreshCookie();
                return Unauthorized();
            }

            var (user, issued) = result.Value;
            SetRefreshCookie(issued.RefreshToken, issued.RefreshTokenExpiresAt);
            return new AuthResponseDto { AccessToken = issued.AccessToken, ExpiresAt = issued.AccessTokenExpiresAt, User = UserDto.From(user) };
        }

        [HttpPost("logout"), AllowAnonymous, EnableRateLimiting(RateLimits.Refresh)]
        public async Task<IActionResult> Logout()
        {
            if (Request.Headers[CsrfHeader] != CsrfValue)
            {
                return BadRequest();
            }
            await _tokens.RevokeAsync(Request.Cookies[TokenService.RefreshCookieName]);
            ClearRefreshCookie();
            return NoContent();
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> Me()
        {
            var user = await _db.Users.AsNoTracking().SingleOrDefaultAsync(a => a.ObjId == CurrentUserId);
            return user == null ? Unauthorized() : UserDto.From(user);
        }

        [HttpPost("password"), EnableRateLimiting(RateLimits.Login)]
        public async Task<IActionResult> ChangePassword([FromBody] PasswordDto data)
        {
            var user = await _db.Users.SingleAsync(a => a.ObjId == CurrentUserId);
            if (!PasswordHashing.Verify(data.CurrentPassword, user.Salt, user.PasswordHash))
            {
                return BadRequest(new { error = "current-password-invalid" });
            }
            var error = UsersController.ValidatePassword(data.NewPassword, user);
            if (error != null)
            {
                return BadRequest(new { error });
            }

            user.Salt = PasswordHashing.GenerateSalt();
            user.PasswordHash = PasswordHashing.Hash(data.NewPassword, user.Salt);
            user.RenewSecurityStamp();
            await _db.SaveChangesAsync();
            await _tokens.RevokeAllForUserAsync(user.ObjId);

            var issued = await _tokens.IssueAsync(user, ClientIp);
            SetRefreshCookie(issued.RefreshToken, issued.RefreshTokenExpiresAt);
            return Ok(new AuthResponseDto { AccessToken = issued.AccessToken, ExpiresAt = issued.AccessTokenExpiresAt, User = UserDto.From(user) });
        }

        private CookieOptions RefreshCookieOptions(DateTimeOffset? expires) => new()
        {
            HttpOnly = true,
            // browsers accept Secure cookies on http://localhost, so this holds for local development too
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = TokenService.RefreshCookiePath,
            Expires = expires,
            IsEssential = true
        };

        private void SetRefreshCookie(string token, DateTime expiresAt)
        {
            Response.Cookies.Append(TokenService.RefreshCookieName, token, RefreshCookieOptions(expiresAt));
        }

        private void ClearRefreshCookie()
        {
            Response.Cookies.Delete(TokenService.RefreshCookieName, RefreshCookieOptions(null));
        }
    }
}
