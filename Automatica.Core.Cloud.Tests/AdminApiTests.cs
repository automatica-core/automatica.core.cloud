using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Automatica.Core.Cloud.EF.Models;
using Microsoft.EntityFrameworkCore;

namespace Automatica.Core.Cloud.Tests
{
    public class AdminApiTests : IClassFixture<CloudFactory>
    {
        private readonly CloudFactory _factory;

        public AdminApiTests(CloudFactory factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("/webapi/v1/license")]
        [InlineData("/webapi/v1/license/features")]
        [InlineData("/webapi/v1/coreServer")]
        [InlineData("/webapi/v1/users")]
        [InlineData("/webapi/v1/plugins")]
        [InlineData("/webapi/v1/deployKeys")]
        [InlineData("/webapi/v1/coreServerVersion")]
        public async Task Admin_endpoints_need_a_token(string url)
        {
            var response = await _factory.Client().GetAsync(url);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Generate_license_needs_admin()
        {
            await _factory.CreateUserAsync("viewer-lic", "viewer-password-1", UserRole.Viewer);
            var viewer = await _factory.LoginAsync("viewer-lic", "viewer-password-1");
            var response = await viewer.PostAsJsonAsync("/webapi/v1/license", new { coreServerId = Guid.NewGuid(), expiresAt = DateTime.UtcNow.AddYears(1), licensedTo = "x", email = "x@example.com" });
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync("/webapi/v1/license")).StatusCode);
        }

        [Fact]
        public async Task Servers_are_scoped_to_their_owner()
        {
            var alice = await _factory.CreateUserAsync("alice", "alice-password-1", UserRole.Viewer);
            var bob = await _factory.CreateUserAsync("bob", "bob-password-123", UserRole.Viewer);
            var (aliceServer, _) = await _factory.CreateServerAsync(alice.ObjId, "alice-server");
            var (bobServer, _) = await _factory.CreateServerAsync(bob.ObjId, "bob-server");

            var aliceClient = await _factory.LoginAsync("alice", "alice-password-1");
            var list = await aliceClient.GetFromJsonAsync<JsonElement>("/webapi/v1/coreServer", CloudFactory.Json);
            Assert.All(list.EnumerateArray(), s => Assert.Equal(alice.ObjId, s.GetProperty("ownerId").GetGuid()));
            Assert.Contains(list.EnumerateArray(), s => s.GetProperty("objId").GetGuid() == aliceServer.ObjId);

            Assert.Equal(HttpStatusCode.NotFound, (await aliceClient.GetAsync($"/webapi/v1/coreServer/{bobServer.ObjId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await aliceClient.PutAsJsonAsync($"/webapi/v1/coreServer/{bobServer.ObjId}", new { serverName = "pwned" })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await aliceClient.PostAsync($"/webapi/v1/coreServer/{bobServer.ObjId}/rotateKey", null)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await aliceClient.DeleteAsync($"/webapi/v1/coreServer/{bobServer.ObjId}")).StatusCode);

            // a viewer cannot assign a server to someone else
            var create = await aliceClient.PostAsJsonAsync("/webapi/v1/coreServer", new { serverName = "x", ownerId = bob.ObjId });
            Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

            // the admin sees everything
            var admin = await _factory.LoginAsync();
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/webapi/v1/coreServer/{bobServer.ObjId}")).StatusCode);
        }

        [Fact]
        public async Task Api_key_is_returned_once_and_never_serialized_afterwards()
        {
            var admin = await _factory.LoginAsync();
            var created = await admin.PostAsJsonAsync("/webapi/v1/coreServer", new { serverName = "once" });
            created.EnsureSuccessStatusCode();
            var body = await created.Content.ReadFromJsonAsync<JsonElement>(CloudFactory.Json);
            var apiKey = body.GetProperty("apiKey").GetGuid();
            var id = body.GetProperty("server").GetProperty("objId").GetGuid();
            Assert.NotEqual(Guid.Empty, apiKey);

            var stored = await _factory.WithDb(db => db.CoreServers.AsNoTracking().SingleAsync(a => a.ObjId == id));
            Assert.Equal(Guid.Empty, stored.ApiKey);
            Assert.Equal(SecretHashing.HashApiKey(apiKey), stored.ApiKeyHash);

            foreach (var url in new[] { "/webapi/v1/coreServer", $"/webapi/v1/coreServer/{id}", "/webapi/v1/license" })
            {
                var text = await admin.GetStringAsync(url);
                Assert.DoesNotContain(apiKey.ToString(), text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("\"apiKey\"", text);
                Assert.DoesNotContain("passwordHash", text);
            }

            var rotated = await admin.PostAsync($"/webapi/v1/coreServer/{id}/rotateKey", null);
            var newKey = (await rotated.Content.ReadFromJsonAsync<JsonElement>(CloudFactory.Json)).GetProperty("apiKey").GetGuid();
            Assert.NotEqual(apiKey, newKey);

            // the old key no longer authenticates the core
            var core = _factory.Client();
            Assert.Equal(HttpStatusCode.Forbidden, (await core.GetAsync($"/webapi/v2/coreServerData/ping/{apiKey}/{stored.ServerGuid}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await core.GetAsync($"/webapi/v2/coreServerData/ping/{newKey}/{stored.ServerGuid}")).StatusCode);
        }

        [Fact]
        public async Task Update_does_not_bind_unexpected_fields()
        {
            var alice = await _factory.CreateUserAsync("mass-assign", "mass-assign-pw1", UserRole.Viewer);
            var (server, _) = await _factory.CreateServerAsync(alice.ObjId, "mine");
            var client = await _factory.LoginAsync("mass-assign", "mass-assign-pw1");

            var response = await client.PutAsJsonAsync($"/webapi/v1/coreServer/{server.ObjId}",
                new { serverName = "renamed", apiKeyHash = "AAAA", serverGuid = Guid.NewGuid(), ownerId = Guid.NewGuid() });
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

            response = await client.PutAsJsonAsync($"/webapi/v1/coreServer/{server.ObjId}", new { serverName = "renamed", apiKeyHash = "AAAA", serverGuid = Guid.NewGuid() });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var stored = await _factory.WithDb(db => db.CoreServers.AsNoTracking().SingleAsync(a => a.ObjId == server.ObjId));
            Assert.Equal("renamed", stored.ServerName);
            Assert.Equal(server.ServerGuid, stored.ServerGuid);
            Assert.Equal(server.ApiKeyHash, stored.ApiKeyHash);
            Assert.Equal(alice.ObjId, stored.This2User);
        }

        [Fact]
        public async Task Users_endpoint_is_sysadmin_only()
        {
            await _factory.CreateUserAsync("plain-admin", "plain-admin-pw1", UserRole.Administrator);
            var client = await _factory.LoginAsync("plain-admin", "plain-admin-pw1");
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/webapi/v1/users")).StatusCode);
        }
    }
}
