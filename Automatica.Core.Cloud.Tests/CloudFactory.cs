using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.RemoteControl;
using Automatica.Core.Cloud.TTS;
using Automatica.Core.Cloud.WebApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Automatica.Core.Cloud.Tests
{
    /// <summary>The real app on an in-memory SQLite database with fake Azure services.</summary>
    public sealed class CloudFactory : WebApplicationFactory<Program>
    {
        public const string AdminUser = "admin";
        public const string AdminPassword = "test-admin-password-123";
        public const string FrpsSecret = "frps-plugin-secret-for-tests";
        public const string FrpsToken = "frps-token-for-tests";

        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        public FakeMailSender Mail { get; } = new();

        /// <summary>Extra settings applied after the defaults (set before the first request).</summary>
        public Dictionary<string, string> Overrides { get; } = new();

        public CloudFactory()
        {
            _connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("Jwt:SigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
            builder.UseSetting("Seed:AdminUserName", AdminUser);
            builder.UseSetting("Seed:AdminPassword", AdminPassword);
            builder.UseSetting("Seed:AdminEmail", "admin@example.com");
            builder.UseSetting("License:PrivateKeyPassword", "license-secret-for-tests");
            builder.UseSetting("Frps:PluginSecret", FrpsSecret);
            builder.UseSetting("Frps:Token", FrpsToken);
            builder.UseSetting("App:PublicBaseUrl", "https://cloud.example.com");
            foreach (var policy in new[] { "login", "refresh", "core-api", "cli", "public", "mail", "tts" })
            {
                builder.UseSetting($"RateLimits:{policy}", "10000");
            }
            foreach (var (key, value) in Overrides)
            {
                builder.UseSetting(key, value);
            }

            builder.ConfigureTestServices(services =>
            {
                services.AddDbContext<CoreContext>(o => o.UseSqlite(_connection));
                services.RemoveAll<IMailSender>();
                services.AddSingleton<IMailSender>(Mail);
                services.RemoveAll<IPackageStore>();
                services.AddSingleton<IPackageStore, FakePackageStore>();
                services.RemoveAll<IDnsManager>();
                services.AddSingleton<IDnsManager, FakeDnsManager>();
                services.RemoveAll<ITextToSpeechService>();
                services.AddSingleton<ITextToSpeechService, FakeTts>();
            });
        }

        public HttpClient Client()
        {
            return CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        }

        public async Task<T> WithDb<T>(Func<CoreContext, Task<T>> action)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoreContext>();
            return await action(db);
        }

        public Task WithDb(Func<CoreContext, Task> action) => WithDb(async db => { await action(db); return true; });

        public async Task<HttpClient> LoginAsync(string userName = AdminUser, string password = AdminPassword)
        {
            var client = Client();
            var response = await client.PostAsJsonAsync("/webapi/v1/auth/login", new { userName, password });
            response.EnsureSuccessStatusCode();
            var auth = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetProperty("accessToken").GetString());
            return client;
        }

        public async Task<User> CreateUserAsync(string userName, string password, UserRole role, bool enabled = true)
        {
            return await WithDb(async db =>
            {
                var salt = PasswordHashing.GenerateSalt();
                var user = new User
                {
                    ObjId = Guid.NewGuid(),
                    UserName = userName,
                    Email = $"{userName}@example.com",
                    Salt = salt,
                    PasswordHash = PasswordHashing.Hash(password, salt),
                    UserRole = role,
                    Enabled = enabled,
                    ApiKey = Guid.NewGuid()
                };
                user.RenewSecurityStamp();
                db.Users.Add(user);
                await db.SaveChangesAsync();
                return user;
            });
        }

        /// <summary>Creates a core server with a hashed api key and returns (server, plaintext key).</summary>
        public async Task<(CoreServer server, Guid apiKey)> CreateServerAsync(Guid ownerId, string name = "server", bool license = true)
        {
            var apiKey = Guid.NewGuid();
            var server = await WithDb(async db =>
            {
                var s = new CoreServer
                {
                    ObjId = Guid.NewGuid(),
                    ServerGuid = Guid.NewGuid(),
                    ServerName = name,
                    This2User = ownerId,
                    ApiKey = Guid.Empty,
                    ApiKeyHash = SecretHashing.HashApiKey(apiKey),
                    ApiKeyCreatedAt = DateTime.UtcNow
                };
                db.CoreServers.Add(s);
                if (license)
                {
                    var keyId = Guid.NewGuid();
                    db.LicenseKeys.Add(new LicenseKey { ObjId = keyId, Version = 1, PrivateKey = "x", PublicKey = "x" });
                    db.Licenses.Add(new License
                    {
                        ObjId = Guid.NewGuid(),
                        This2CoreServer = s.ObjId,
                        This2VersionKey = keyId,
                        ExpiresAt = DateTime.UtcNow.AddYears(1),
                        LicenseKey = "license-xml",
                        AllowRemoteControl = true,
                        AllowTextToSpeech = true,
                        MaxRemoteTunnels = 5,
                        LicensedTo = name
                    });
                }
                await db.SaveChangesAsync();
                return s;
            });
            return (server, apiKey);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }

    public sealed class FakeMailSender : IMailSender
    {
        public ConcurrentBag<(string to, string subject, string text, string html)> Sent { get; } = new();

        public Task SendAsync(string to, string subject, string plainText, string html, CancellationToken token = default)
        {
            Sent.Add((to, subject, plainText, html));
            return Task.CompletedTask;
        }
    }

    public sealed class FakePackageStore : IPackageStore
    {
        public Task<Uri> UploadAsync(string name, Stream content, IDictionary<string, string> metadata, CancellationToken token = default)
            => Task.FromResult(new Uri("https://storage.example.com/" + name));

        public Task DeleteAsync(string name, CancellationToken token = default) => Task.CompletedTask;
    }

    public sealed class FakeDnsManager : IDnsManager
    {
        public Task<bool> RemoveDnsNameAsync(string name, Guid serverId, Guid? pluginGuid, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> IsDnsNameAvailableAsync(string name, Guid serverId, Guid? pluginGuid, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<(string url, string subDomain)> CreateDnsNameAsync(string name, Guid serverId, Guid? pluginGuid, CancellationToken cancellationToken = default)
            => Task.FromResult(($"https://{name}.dev.automaticaremote.com", name));
    }

    public sealed class FakeTts : ITextToSpeechService
    {
        public Task<SpeechSynthesizeResponse> TextToSpeech(Guid serverId, Guid id, string text, string language, string voice, CancellationToken token = default)
            => Task.FromResult(new SpeechSynthesizeResponse { Uri = "https://storage.example.com/tts/" + id, AudioDuration = TimeSpan.FromSeconds(1) });
    }
}
