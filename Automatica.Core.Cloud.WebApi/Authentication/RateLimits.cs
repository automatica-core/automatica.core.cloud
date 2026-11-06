using System;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Automatica.Core.Cloud.WebApi.Authentication
{
    /// <summary>
    /// Per-IP limits for credential endpoints and per-server limits for the costly core endpoints (mail, tts).
    /// Limits are in-memory per instance, which is fine for the single-instance App Service plan.
    /// </summary>
    public static class RateLimits
    {
        public const string Login = "login";
        public const string Refresh = "refresh";
        public const string CoreApi = "core-api";
        public const string Cli = "cli";
        public const string Mail = "mail";
        public const string Tts = "tts";
        public const string Public = "public";
        public const string Frps = "frps";

        public static string ClientIp(HttpContext context)
        {
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private static string ServerPartition(HttpContext context)
        {
            return context.Request.RouteValues["serverGuid"]?.ToString()
                   ?? context.Request.RouteValues["apiKey"]?.ToString()
                   ?? ClientIp(context);
        }

        public static IServiceCollection AddCloudRateLimits(this IServiceCollection services, IConfiguration configuration)
        {
            // optional per-policy override, e.g. RateLimits:login = 20
            int Permit(string policy, int fallback) => configuration?.GetValue<int?>($"RateLimits:{policy}") ?? fallback;

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy(Login, context => RateLimitPartition.GetFixedWindowLimiter(ClientIp(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = Permit(Login, 10), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

                options.AddPolicy(Refresh, context => RateLimitPartition.GetFixedWindowLimiter(ClientIp(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = Permit(Refresh, 60), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

                options.AddPolicy(CoreApi, context => RateLimitPartition.GetFixedWindowLimiter(ClientIp(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = Permit(CoreApi, 300), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

                options.AddPolicy(Cli, context => RateLimitPartition.GetFixedWindowLimiter(ClientIp(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = Permit(Cli, 120), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

                options.AddPolicy(Public, context => RateLimitPartition.GetFixedWindowLimiter(ClientIp(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = Permit(Public, 20), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

                options.AddPolicy(Frps, context => RateLimitPartition.GetFixedWindowLimiter(ClientIp(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = Permit(Frps, 3000), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

                options.AddPolicy(Mail, context => RateLimitPartition.GetFixedWindowLimiter("mail:" + ServerPartition(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = Permit(Mail, 20), Window = TimeSpan.FromHours(1), QueueLimit = 0 }));

                options.AddPolicy(Tts, context => RateLimitPartition.GetFixedWindowLimiter("tts:" + ServerPartition(context),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = Permit(Tts, 200), Window = TimeSpan.FromHours(1), QueueLimit = 0 }));
            });
            return services;
        }
    }
}
