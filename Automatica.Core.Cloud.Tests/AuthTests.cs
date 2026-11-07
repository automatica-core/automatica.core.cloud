using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Automatica.Core.Cloud.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Automatica.Core.Cloud.Tests
{
    public class AuthTests : IClassFixture<CloudFactory>
    {
        private readonly CloudFactory _factory;

        public AuthTests(CloudFactory factory)
        {
            _factory = factory;
        }

        private static HttpRequestMessage Csrf(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add("X-Requested-With", "automatica-cloud");
            return request;
        }

        [Fact]
        public async Task Seeded_admin_can_log_in_and_read_me()
        {
            var client = await _factory.LoginAsync();
            var me = await client.GetFromJsonAsync<JsonElement>("/webapi/v1/auth/me", CloudFactory.Json);
            Assert.Equal(CloudFactory.AdminUser, me.GetProperty("userName").GetString());
            Assert.False(me.TryGetProperty("passwordHash", out _));
            Assert.False(me.TryGetProperty("apiKey", out _));
        }

        [Fact]
        public async Task Wrong_password_is_rejected()
        {
            var response = await _factory.Client().PostAsJsonAsync("/webapi/v1/auth/login", new { userName = CloudFactory.AdminUser, password = "wrong-password-xyz" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Disabled_user_cannot_log_in()
        {
            await _factory.CreateUserAsync("disabled-user", "disabled-password-1", UserRole.Administrator, enabled: false);
            var response = await _factory.Client().PostAsJsonAsync("/webapi/v1/auth/login", new { userName = "disabled-user", password = "disabled-password-1" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Token_of_user_disabled_afterwards_is_rejected()
        {
            var user = await _factory.CreateUserAsync("later-disabled", "later-disabled-pw1", UserRole.Administrator);
            var client = await _factory.LoginAsync("later-disabled", "later-disabled-pw1");
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/webapi/v1/auth/me")).StatusCode);

            await _factory.WithDb(async db =>
            {
                var u = await db.Users.SingleAsync(a => a.ObjId == user.ObjId);
                u.Enabled = false;
                await db.SaveChangesAsync();
            });

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/webapi/v1/auth/me")).StatusCode);
        }

        [Fact]
        public async Task Role_comes_from_the_database_not_the_token()
        {
            var user = await _factory.CreateUserAsync("demoted", "demoted-password1", UserRole.SystemAdministrator);
            var client = await _factory.LoginAsync("demoted", "demoted-password1");
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/webapi/v1/users")).StatusCode);

            await _factory.WithDb(async db =>
            {
                var u = await db.Users.SingleAsync(a => a.ObjId == user.ObjId);
                u.UserRole = UserRole.Viewer;
                await db.SaveChangesAsync();
            });

            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/webapi/v1/users")).StatusCode);
        }

        [Fact]
        public async Task Token_signed_with_another_key_or_issuer_is_rejected()
        {
            var client = _factory.Client();
            var admin = await _factory.WithDb(db => db.Users.SingleAsync(a => a.UserName == CloudFactory.AdminUser));

            string Forge(string issuer, byte[] key)
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.CreateToken(new SecurityTokenDescriptor
                {
                    Issuer = issuer,
                    Audience = "automatica-cloud-web",
                    Expires = DateTime.UtcNow.AddMinutes(5),
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim("sub", admin.ObjId.ToString()),
                        new Claim("role", "SystemAdministrator"),
                        new Claim("sst", admin.SecurityStamp)
                    }),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
                });
                return handler.WriteToken(token);
            }

            // the old hard-coded key from git history must not work any more
            var oldKey = System.Text.Encoding.ASCII.GetBytes("this-is-a-very-secret-code-with-a-lot-of-chars");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Forge("automatica-cloud", oldKey));
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/webapi/v1/auth/me")).StatusCode);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Forge("someone-else", RandomNumberGenerator.GetBytes(32)));
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/webapi/v1/auth/me")).StatusCode);
        }

        [Fact]
        public async Task Refresh_rotates_and_reuse_revokes_the_family()
        {
            var client = _factory.Client();
            var login = await client.PostAsJsonAsync("/webapi/v1/auth/login", new { userName = CloudFactory.AdminUser, password = CloudFactory.AdminPassword });
            login.EnsureSuccessStatusCode();
            var firstCookie = login.Headers.GetValues("Set-Cookie").Single(a => a.StartsWith("acc_rt="));
            Assert.Contains("httponly", firstCookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=strict", firstCookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("secure", firstCookie, StringComparison.OrdinalIgnoreCase);

            // without the CSRF header the refresh is refused
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/webapi/v1/auth/refresh", null)).StatusCode);

            var refreshed = await client.SendAsync(Csrf(HttpMethod.Post, "/webapi/v1/auth/refresh"));
            Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);

            // replaying the first (already rotated) refresh token revokes the whole family
            var replay = _factory.Client();
            var request = Csrf(HttpMethod.Post, "/webapi/v1/auth/refresh");
            request.Headers.Add("Cookie", firstCookie.Split(';')[0]);
            Assert.Equal(HttpStatusCode.Unauthorized, (await replay.SendAsync(request)).StatusCode);

            // ...so the current token of the original client is dead too
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(Csrf(HttpMethod.Post, "/webapi/v1/auth/refresh"))).StatusCode);
        }

        [Fact]
        public async Task Login_is_rate_limited_per_ip()
        {
            using var factory = new CloudFactory();
            factory.Overrides["RateLimits:login"] = "3";
            var client = factory.Client();
            HttpStatusCode last = HttpStatusCode.OK;
            for (var i = 0; i < 5; i++)
            {
                last = (await client.PostAsJsonAsync("/webapi/v1/auth/login", new { userName = "nobody", password = "nothing-at-all" })).StatusCode;
            }
            Assert.Equal(HttpStatusCode.TooManyRequests, last);
        }

        [Fact]
        public async Task Leaked_credentials_are_invalidated()
        {
            var leakedKey = Guid.NewGuid();
            const string leakedPassword = "old-leaked-password";
            var salt = PasswordHashing.GenerateSalt();
            var user = new User
            {
                ObjId = Guid.NewGuid(),
                UserName = "old-sa",
                Salt = salt,
                PasswordHash = PasswordHashing.LegacyHash(leakedPassword, salt),
                ApiKey = leakedKey,
                Enabled = true,
                UserRole = UserRole.SystemAdministrator
            };
            await _factory.WithDb(async db =>
            {
                db.Users.Add(user);
                await db.SaveChangesAsync();
                await EF.Helper.DatabaseInitializer.RemoveLeakedCredentialsAsync(db, Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance,
                    SecretHashing.HashApiKey(leakedKey), leakedPassword, default);
            });

            var stored = await _factory.WithDb(db => db.Users.AsNoTracking().SingleAsync(a => a.ObjId == user.ObjId));
            Assert.NotEqual(leakedKey, stored.ApiKey);
            Assert.False(PasswordHashing.IsUsable(stored.PasswordHash));
            Assert.False(string.IsNullOrEmpty(stored.SecurityStamp));

            var response = await _factory.Client().PostAsJsonAsync("/webapi/v1/auth/login", new { userName = "old-sa", password = leakedPassword });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Legacy_password_hash_is_upgraded_on_login()
        {
            var salt = PasswordHashing.GenerateSalt();
            await _factory.WithDb(async db =>
            {
                db.Users.Add(new User
                {
                    ObjId = Guid.NewGuid(), UserName = "legacy-user", Salt = salt, Enabled = true, UserRole = UserRole.Viewer,
                    PasswordHash = PasswordHashing.LegacyHash("legacy-password-1", salt), ApiKey = Guid.NewGuid()
                });
                await db.SaveChangesAsync();
            });

            await _factory.LoginAsync("legacy-user", "legacy-password-1");
            var stored = await _factory.WithDb(db => db.Users.AsNoTracking().SingleAsync(a => a.UserName == "legacy-user"));
            Assert.False(PasswordHashing.IsLegacy(stored.PasswordHash));
            Assert.True(PasswordHashing.Verify("legacy-password-1", stored.Salt, stored.PasswordHash));
        }
    }
}
