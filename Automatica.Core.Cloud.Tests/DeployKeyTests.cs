using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Automatica.Core.Cloud.EF.Models;
using Microsoft.EntityFrameworkCore;

namespace Automatica.Core.Cloud.Tests
{
    public class DeployKeyTests : IClassFixture<CloudFactory>
    {
        private readonly CloudFactory _factory;

        public DeployKeyTests(CloudFactory factory)
        {
            _factory = factory;
        }

        private async Task<string> CreateKey(string[] scopes, string branches = "develop")
        {
            var admin = await _factory.LoginAsync();
            var response = await admin.PostAsJsonAsync("/webapi/v1/deployKeys", new { name = "ci", scopes, allowedBranches = branches });
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(CloudFactory.Json);
            return body.GetProperty("key").GetString();
        }

        private static object Docker(string version) => new { objId = Guid.NewGuid(), version, imageName = "automaticacore/automatica", imageTag = version, isPublic = true };

        [Fact]
        public async Task User_api_key_no_longer_works_for_the_cli()
        {
            var admin = await _factory.WithDb(db => db.Users.AsNoTracking().SingleAsync(a => a.UserName == CloudFactory.AdminUser));
            var response = await _factory.Client().PostAsJsonAsync($"/webapi/v1/coreCliData/deployDocker/develop/{admin.ApiKey}", Docker("10.0.0.1"));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Scope_is_enforced()
        {
            var key = await CreateKey(new[] { "ReadPlugins" });
            var client = _factory.Client();
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/webapi/v1/coreCliData/plugins/10.0.0.0/{key}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/webapi/v1/coreCliData/deployDocker/develop/{key}", Docker("10.0.0.2"))).StatusCode);
        }

        [Fact]
        public async Task Branch_is_enforced_and_header_works()
        {
            var key = await CreateKey(new[] { "DeployDocker" }, "develop");
            var client = _factory.Client();
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync($"/webapi/v1/coreCliData/deployDocker/develop/{key}", Docker("10.0.0.3"))).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/webapi/v1/coreCliData/deployDocker/master/{key}", Docker("10.0.0.4"))).StatusCode);

            var request = new HttpRequestMessage(HttpMethod.Post, "/webapi/v1/coreCliData/deployDocker/develop/-") { Content = JsonContent.Create(Docker("10.0.0.5")) };
            request.Headers.Add("X-Automatica-Deploy-Key", key);
            Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(request)).StatusCode);
        }

        [Fact]
        public async Task Revoked_key_is_rejected_and_key_is_stored_hashed()
        {
            var admin = await _factory.LoginAsync();
            var response = await admin.PostAsJsonAsync("/webapi/v1/deployKeys", new { name = "revoke-me", scopes = new[] { "ReadPlugins" }, allowedBranches = "*" });
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(CloudFactory.Json);
            var key = body.GetProperty("key").GetString();
            var id = body.GetProperty("deployKey").GetProperty("objId").GetGuid();

            var list = await admin.GetStringAsync("/webapi/v1/deployKeys");
            Assert.DoesNotContain(key, list);
            var stored = await _factory.WithDb(db => db.DeployKeys.AsNoTracking().SingleAsync(a => a.ObjId == id));
            Assert.Equal(SecretHashing.Sha256(key), stored.KeyHash);

            (await admin.PostAsync($"/webapi/v1/deployKeys/{id}/revoke", null)).EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Forbidden, (await _factory.Client().GetAsync($"/webapi/v1/coreCliData/plugins/10.0.0.0/{key}")).StatusCode);
        }
    }
}
