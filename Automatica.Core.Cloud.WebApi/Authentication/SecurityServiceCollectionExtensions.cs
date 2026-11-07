using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Automatica.Core.Cloud.EF.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Automatica.Core.Cloud.WebApi.Authentication
{
    public static class SecurityServiceCollectionExtensions
    {
        public static IServiceCollection AddCloudSecurity(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            var jwt = configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
            services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));
            services.AddSingleton(sp => new SigningKeyProvider(jwt, environment, sp.GetService<ILogger<SigningKeyProvider>>()));
            services.AddScoped<TokenService>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();

            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<SigningKeyProvider>((options, keys) =>
                {
                    options.RequireHttpsMetadata = !environment.IsDevelopment() && !environment.IsEnvironment("Testing");
                    options.MapInboundClaims = false;
                    options.SaveToken = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = keys.Key,
                        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwt.Audience,
                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),
                        NameClaimType = Policies.NameClaim,
                        RoleClaimType = Policies.RoleClaim
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = ValidateUserAgainstDatabase
                    };
                });

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build();
                options.FallbackPolicy = options.DefaultPolicy;

                options.AddPolicy(Policies.Admin, policy => policy.RequireAuthenticatedUser()
                    .RequireClaim(Policies.RoleClaim, nameof(UserRole.Administrator), nameof(UserRole.SystemAdministrator)));
                options.AddPolicy(Policies.SysAdmin, policy => policy.RequireAuthenticatedUser()
                    .RequireClaim(Policies.RoleClaim, nameof(UserRole.SystemAdministrator)));
            });

            services.AddCloudRateLimits(configuration);
            return services;
        }

        /// <summary>
        /// The token only proves who the caller is. Role and enabled state are read from the database on every
        /// request, and the security stamp revokes tokens issued before a password/role change.
        /// </summary>
        private static async Task ValidateUserAgainstDatabase(TokenValidatedContext context)
        {
            var principal = context.Principal;
            var userId = principal.GetUserId();
            var stamp = principal?.FindFirst(Policies.StampClaim)?.Value;

            var db = context.HttpContext.RequestServices.GetRequiredService<CoreContext>();
            var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(a => a.ObjId == userId, context.HttpContext.RequestAborted);

            if (user == null || !user.Enabled || string.IsNullOrEmpty(stamp) || user.SecurityStamp != stamp)
            {
                context.Fail("user disabled or token revoked");
                return;
            }

            var claims = new List<Claim>
            {
                new(Policies.SubjectClaim, user.ObjId.ToString()),
                new(Policies.NameClaim, user.UserName ?? string.Empty),
                new(Policies.RoleClaim, user.UserRole.ToString()),
                new(Policies.StampClaim, user.SecurityStamp)
            };
            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme, Policies.NameClaim, Policies.RoleClaim));
        }
    }
}
