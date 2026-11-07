using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Cloud.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Cloud.EF.Helper
{
    /// <summary>
    /// Runs on every start: applies migrations, removes credentials that were published in the public repo,
    /// back-fills hashed api keys and seeds the first administrator from configuration.
    /// </summary>
    public static class DatabaseInitializer
    {
        // SHA-256 of the SystemAdministrator ApiKey that older versions of this repo seeded (and that leaked via CI scripts).
        private static readonly byte[] LeakedSeedApiKeyHash = Convert.FromHexString("0bb646b2c2df38ad0b9bea5f2b1c3db9bb5c594447a508291ba17342ce3b84f1");

        // The password older versions of this repo seeded for "sa". It is public (git history), so it is only kept here to detect it.
        private const string LeakedSeedPassword = "thisisaverysecurepassword123$";

        public static async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken token = default)
        {
            using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CoreContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

            if (dbContext.Database.IsRelational() && dbContext.Database.ProviderName!.Contains("SqlServer", StringComparison.Ordinal))
            {
                await dbContext.Database.MigrateAsync(token);
            }
            else
            {
                await dbContext.Database.EnsureCreatedAsync(token);
            }

            await RemoveLeakedCredentialsAsync(dbContext, logger, token);
            await BackfillApiKeyHashesAsync(dbContext, logger, token);
            await SeedAdministratorAsync(dbContext, config, logger, token);
            await CleanupRefreshTokensAsync(dbContext, token);
        }

        internal static Task RemoveLeakedCredentialsAsync(CoreContext dbContext, ILogger logger, CancellationToken token)
        {
            return RemoveLeakedCredentialsAsync(dbContext, logger, LeakedSeedApiKeyHash, LeakedSeedPassword, token);
        }

        internal static async Task RemoveLeakedCredentialsAsync(CoreContext dbContext, ILogger logger, byte[] leakedApiKeyHash, string leakedPassword, CancellationToken token)
        {
            var users = await dbContext.Users.ToListAsync(token);
            foreach (var user in users)
            {
                var changed = false;
                if (SecretHashing.FixedTimeEquals(SecretHashing.HashApiKey(user.ApiKey), leakedApiKeyHash))
                {
                    user.ApiKey = Guid.NewGuid();
                    changed = true;
                    logger.LogWarning("Replaced the leaked seeded api key of user {UserId}", user.ObjId);
                }

                if (PasswordHashing.IsUsable(user.PasswordHash) && !string.IsNullOrEmpty(user.Salt) &&
                    PasswordHashing.Verify(leakedPassword, user.Salt, user.PasswordHash))
                {
                    user.PasswordHash = PasswordHashing.Unusable();
                    user.RenewSecurityStamp();
                    changed = true;
                    logger.LogWarning("User {UserId} still had the leaked seeded password; the password was disabled. Set Seed:AdminPassword to set a new one.", user.ObjId);
                }

                if (string.IsNullOrEmpty(user.SecurityStamp))
                {
                    user.RenewSecurityStamp();
                    changed = true;
                }

                if (changed)
                {
                    dbContext.Users.Update(user);
                }
            }

            // A plugin, version or docker image published with the leaked key cannot be told apart in the data;
            // terraform/README.md lists the manual review.
            await dbContext.SaveChangesAsync(token);
        }

        internal static async Task BackfillApiKeyHashesAsync(CoreContext dbContext, ILogger logger, CancellationToken token)
        {
            var servers = await dbContext.CoreServers.Where(a => a.ApiKeyHash == null).ToListAsync(token);
            foreach (var server in servers)
            {
                if (server.ApiKey == Guid.Empty)
                {
                    continue;
                }
                server.ApiKeyHash = SecretHashing.HashApiKey(server.ApiKey);
                server.ApiKeyCreatedAt ??= DateTime.UtcNow;
            }

            if (servers.Count > 0)
            {
                await dbContext.SaveChangesAsync(token);
                logger.LogInformation("Back-filled api key hashes for {Count} core servers", servers.Count);
            }
        }

        internal static async Task SeedAdministratorAsync(CoreContext dbContext, IConfiguration config, ILogger logger, CancellationToken token)
        {
            var userName = config["Seed:AdminUserName"];
            if (string.IsNullOrWhiteSpace(userName))
            {
                userName = "admin";
            }
            var configuredPassword = config["Seed:AdminPassword"];

            if (!await dbContext.Users.AnyAsync(token))
            {
                var password = configuredPassword;
                var generated = false;
                if (string.IsNullOrEmpty(password))
                {
                    password = PasswordHashing.GeneratePassword();
                    generated = true;
                }

                var salt = PasswordHashing.GenerateSalt();
                var admin = new User
                {
                    ObjId = Guid.NewGuid(),
                    UserName = userName,
                    FirstName = "Administrator",
                    LastName = "",
                    Email = config["Seed:AdminEmail"],
                    Salt = salt,
                    PasswordHash = PasswordHashing.Hash(password, salt),
                    UserRole = UserRole.SystemAdministrator,
                    Enabled = true,
                    ApiKey = Guid.NewGuid(),
                    PublisherName = config["Seed:PublisherName"] ?? "automatica.core"
                };
                admin.RenewSecurityStamp();
                dbContext.Users.Add(admin);
                await dbContext.SaveChangesAsync(token);

                if (generated)
                {
                    // printed once to stdout only, on the very first start of an empty database
                    Console.WriteLine($"[automatica-cloud] created administrator '{userName}' with generated password: {password}");
                    Console.WriteLine("[automatica-cloud] change it after the first login.");
                }
                else
                {
                    logger.LogInformation("Created administrator {UserName} from configuration", userName);
                }
                return;
            }

            // Existing database: Seed:AdminPassword only (re)sets the password of the configured administrator
            // while that account has no usable password (e.g. after the leaked password was disabled).
            if (string.IsNullOrEmpty(configuredPassword))
            {
                return;
            }

            var existing = await dbContext.Users.SingleOrDefaultAsync(a => a.UserName == userName, token);
            if (existing == null || existing.HasUsablePassword)
            {
                return;
            }

            if (configuredPassword.Length < PasswordHashing.MinPasswordLength)
            {
                logger.LogError("Seed:AdminPassword is shorter than {Length} characters; not applied", PasswordHashing.MinPasswordLength);
                return;
            }

            existing.Salt = PasswordHashing.GenerateSalt();
            existing.PasswordHash = PasswordHashing.Hash(configuredPassword, existing.Salt);
            existing.Enabled = true;
            existing.FailedLoginCount = 0;
            existing.LockedUntil = null;
            existing.RenewSecurityStamp();
            await dbContext.SaveChangesAsync(token);
            logger.LogWarning("Set the password of {UserName} from Seed:AdminPassword. Remove the setting after logging in.", userName);
        }

        private static async Task CleanupRefreshTokensAsync(CoreContext dbContext, CancellationToken token)
        {
            var cutoff = DateTime.UtcNow.AddDays(-30);
            var old = await dbContext.RefreshTokens.Where(a => a.ExpiresAt < cutoff).ToListAsync(token);
            if (old.Count > 0)
            {
                dbContext.RefreshTokens.RemoveRange(old);
                await dbContext.SaveChangesAsync(token);
            }
        }
    }
}
