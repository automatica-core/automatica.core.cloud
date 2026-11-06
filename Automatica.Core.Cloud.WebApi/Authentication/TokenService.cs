using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Cloud.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Automatica.Core.Cloud.WebApi.Authentication
{
    public record IssuedTokens(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt);

    /// <summary>
    /// Short-lived access tokens (JWT, default 15 min) plus rotating refresh tokens (random, hashed in the DB,
    /// delivered as an HttpOnly cookie). Reusing an already rotated refresh token revokes the whole login.
    /// </summary>
    public class TokenService
    {
        public const string RefreshCookieName = "acc_rt";
        public const string RefreshCookiePath = "/webapi/v1/auth";

        private readonly CoreContext _db;
        private readonly JwtOptions _options;
        private readonly SigningKeyProvider _keys;

        public TokenService(CoreContext db, IOptions<JwtOptions> options, SigningKeyProvider keys)
        {
            _db = db;
            _options = options.Value;
            _keys = keys;
        }

        public string CreateAccessToken(User user, out DateTime expiresAt)
        {
            expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
            var claims = new Dictionary<string, object>
            {
                [Policies.SubjectClaim] = user.ObjId.ToString(),
                [Policies.NameClaim] = user.UserName ?? string.Empty,
                [Policies.RoleClaim] = user.UserRole.ToString(),
                [Policies.StampClaim] = user.SecurityStamp ?? string.Empty,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N")
            };

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                Claims = claims,
                IssuedAt = DateTime.UtcNow,
                NotBefore = DateTime.UtcNow,
                Expires = expiresAt,
                SigningCredentials = new SigningCredentials(_keys.Key, SecurityAlgorithms.HmacSha256)
            };
            return new JsonWebTokenHandler().CreateToken(descriptor);
        }

        public async Task<IssuedTokens> IssueAsync(User user, string ip, Guid? familyId = null, CancellationToken token = default)
        {
            var access = CreateAccessToken(user, out var accessExpires);
            var (refresh, entity) = NewRefreshToken(user, ip, familyId ?? Guid.NewGuid());
            _db.RefreshTokens.Add(entity);
            await _db.SaveChangesAsync(token);
            return new IssuedTokens(access, accessExpires, refresh, entity.ExpiresAt);
        }

        private (string raw, RefreshToken entity) NewRefreshToken(User user, string ip, Guid familyId)
        {
            var raw = SecretHashing.NewToken("", 48);
            var entity = new RefreshToken
            {
                ObjId = Guid.NewGuid(),
                This2User = user.ObjId,
                FamilyId = familyId,
                TokenHash = SecretHashing.Sha256(raw),
                SecurityStamp = user.SecurityStamp,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenDays),
                CreatedByIp = Truncate(ip, 64)
            };
            return (raw, entity);
        }

        /// <summary>Returns null when the token is unknown, expired, revoked or the user is no longer valid.</summary>
        public async Task<(User user, IssuedTokens tokens)?> RefreshAsync(string rawRefreshToken, string ip, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(rawRefreshToken) || rawRefreshToken.Length > 200)
            {
                return null;
            }

            var hash = SecretHashing.Sha256(rawRefreshToken);
            var stored = await _db.RefreshTokens.SingleOrDefaultAsync(a => a.TokenHash == hash, token);
            if (stored == null)
            {
                return null;
            }

            if (stored.RevokedAt.HasValue)
            {
                // a rotated token was presented again: assume it was stolen and end the whole login
                await RevokeFamilyAsync(stored.FamilyId, token);
                return null;
            }

            if (stored.ExpiresAt <= DateTime.UtcNow)
            {
                return null;
            }

            var user = await _db.Users.SingleOrDefaultAsync(a => a.ObjId == stored.This2User, token);
            if (user == null || !user.Enabled || user.SecurityStamp != stored.SecurityStamp)
            {
                await RevokeFamilyAsync(stored.FamilyId, token);
                return null;
            }

            var access = CreateAccessToken(user, out var accessExpires);
            var (raw, next) = NewRefreshToken(user, ip, stored.FamilyId);
            stored.RevokedAt = DateTime.UtcNow;
            stored.ReplacedBy = next.ObjId;
            _db.RefreshTokens.Add(next);
            await _db.SaveChangesAsync(token);

            return (user, new IssuedTokens(access, accessExpires, raw, next.ExpiresAt));
        }

        public async Task RevokeAsync(string rawRefreshToken, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(rawRefreshToken))
            {
                return;
            }
            var hash = SecretHashing.Sha256(rawRefreshToken);
            var stored = await _db.RefreshTokens.SingleOrDefaultAsync(a => a.TokenHash == hash, token);
            if (stored != null)
            {
                await RevokeFamilyAsync(stored.FamilyId, token);
            }
        }

        public async Task RevokeAllForUserAsync(Guid userId, CancellationToken token = default)
        {
            var tokens = await _db.RefreshTokens.Where(a => a.This2User == userId && a.RevokedAt == null).ToListAsync(token);
            foreach (var t in tokens)
            {
                t.RevokedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync(token);
        }

        private async Task RevokeFamilyAsync(Guid familyId, CancellationToken token)
        {
            var tokens = await _db.RefreshTokens.Where(a => a.FamilyId == familyId && a.RevokedAt == null).ToListAsync(token);
            foreach (var t in tokens)
            {
                t.RevokedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync(token);
        }

        private static string Truncate(string value, int length)
        {
            if (value == null)
            {
                return null;
            }
            return value.Length <= length ? value : value[..length];
        }
    }
}
