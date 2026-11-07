using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Automatica.Core.Cloud.EF.Models;
using Microsoft.EntityFrameworkCore;

namespace Automatica.Core.Cloud.Tests
{
    public class CoreApiTests : IClassFixture<CloudFactory>
    {
        private readonly CloudFactory _factory;

        public CoreApiTests(CloudFactory factory)
        {
            _factory = factory;
        }

        private Task<User> Owner(string name) => _factory.CreateUserAsync(name, name + "-password-1", UserRole.Viewer);

        [Fact]
        public async Task V2_needs_matching_key_and_server_guid()
        {
            var owner = await Owner("v2-owner");
            var (server, key) = await _factory.CreateServerAsync(owner.ObjId);
            var (other, otherKey) = await _factory.CreateServerAsync(owner.ObjId);
            var client = _factory.Client();

            Assert.Equal("{\"Response\": \"pong\"}", await client.GetStringAsync($"/webapi/v2/coreServerData/ping/{key}/{server.ServerGuid}"));
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/webapi/v2/coreServerData/ping/{Guid.NewGuid()}/{server.ServerGuid}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/webapi/v2/coreServerData/ping/{otherKey}/{server.ServerGuid}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/webapi/v2/coreServerData/ping/not-a-guid/{server.ServerGuid}")).StatusCode);
        }

        [Fact]
        public async Task Headers_win_over_the_path_segments()
        {
            var owner = await Owner("header-owner");
            var (server, key) = await _factory.CreateServerAsync(owner.ObjId);
            var client = _factory.Client();

            HttpRequestMessage Ping(string path, Guid? headerKey, Guid? headerGuid)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, path);
                if (headerKey.HasValue) request.Headers.Add("X-Automatica-ApiKey", headerKey.Value.ToString());
                if (headerGuid.HasValue) request.Headers.Add("X-Automatica-ServerGuid", headerGuid.Value.ToString());
                return request;
            }

            // path segments are placeholders, the headers authenticate
            Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Ping("/webapi/v2/coreServerData/ping/-/-", key, server.ServerGuid))).StatusCode);
            // a correct key in the path does not help when the header carries a wrong one
            Assert.Equal(HttpStatusCode.Forbidden,
                (await client.SendAsync(Ping($"/webapi/v2/coreServerData/ping/{key}/{server.ServerGuid}", Guid.NewGuid(), server.ServerGuid))).StatusCode);
            // key from the header, guid from the path
            Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Ping($"/webapi/v2/coreServerData/ping/-/{server.ServerGuid}", key, null))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(Ping("/webapi/v1/coreServerData/checkLicense/-", key, null))).StatusCode);
        }

        [Fact]
        public async Task V1_accepts_the_key_alone()
        {
            var owner = await Owner("v1-owner");
            var (_, key) = await _factory.CreateServerAsync(owner.ObjId);
            var client = _factory.Client();
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/webapi/v1/coreServerData/checkLicense/{key}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/webapi/v1/coreServerData/checkLicense/{Guid.NewGuid()}")).StatusCode);
        }

        [Fact]
        public async Task License_v2_returns_the_key_for_the_right_server_only()
        {
            var owner = await Owner("lic-owner");
            var (server, key) = await _factory.CreateServerAsync(owner.ObjId);
            var client = _factory.Client();
            Assert.Equal("license-xml", await client.GetStringAsync($"/webapi/v2/coreServerData/license/{key}/{server.ServerGuid}"));
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/webapi/v2/coreServerData/license/{Guid.NewGuid()}/{server.ServerGuid}")).StatusCode);
        }

        [Fact]
        public async Task SayHello_keeps_the_payload_and_updates_the_server()
        {
            var owner = await Owner("hello-owner");
            var (server, key) = await _factory.CreateServerAsync(owner.ObjId);
            var response = await _factory.Client().PostAsJsonAsync($"/webapi/v2/coreServerData/sayHello/{key}/{server.ServerGuid}",
                new { rid = "linux-arm64", version = "10.1.0.1", serverGuid = server.ServerGuid });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var stored = await _factory.WithDb(db => db.CoreServers.AsNoTracking().SingleAsync(a => a.ObjId == server.ObjId));
            Assert.Equal("10.1.0.1", stored.Version);
            Assert.Equal("linux-arm64", stored.Rid);
        }

        [Fact]
        public async Task SendMail_only_reaches_the_owner_and_verified_recipients()
        {
            var owner = await Owner("mail-owner");
            var (server, key) = await _factory.CreateServerAsync(owner.ObjId, "mail-server");
            await _factory.WithDb(async db =>
            {
                db.MailRecipients.Add(new MailRecipient { ObjId = Guid.NewGuid(), This2CoreServer = server.ObjId, Email = "verified@example.com", VerifiedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow });
                db.MailRecipients.Add(new MailRecipient { ObjId = Guid.NewGuid(), This2CoreServer = server.ObjId, Email = "pending@example.com", CreatedAt = DateTime.UtcNow });
                await db.SaveChangesAsync();
            });

            var response = await _factory.Client().PostAsJsonAsync($"/webapi/v2/coreServerData/sendMail/{key}/{server.ServerGuid}", new
            {
                to = new[] { "mail-owner@example.com", "verified@example.com", "pending@example.com", "victim@example.org" },
                subject = "Alarm\r\nBcc: x@example.org",
                body = "<a href=\"https://evil.example\">click</a>"
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            if (result.ValueKind == JsonValueKind.String)
            {
                result = JsonDocument.Parse(result.GetString()!).RootElement;
            }
            Assert.True(result.GetProperty("Result").GetBoolean());
            var skipped = result.GetProperty("Skipped").EnumerateArray().Select(a => a.GetString()).ToList();
            Assert.Contains("pending@example.com", skipped);
            Assert.Contains("victim@example.org", skipped);

            var sent = _factory.Mail.Sent.Where(a => a.html.Contains("mail-server")).ToList();
            Assert.Equal(new[] { "mail-owner@example.com", "verified@example.com" }, sent.Select(a => a.to).OrderBy(a => a));
            Assert.All(sent, m =>
            {
                Assert.DoesNotContain("\n", m.subject);
                Assert.DoesNotContain("<a href", m.html);
            });
        }

        [Fact]
        public async Task CreateRemoteConnect_validates_the_subdomain_and_returns_the_frps_token()
        {
            var owner = await Owner("remote-owner");
            var (server, key) = await _factory.CreateServerAsync(owner.ObjId);
            var client = _factory.Client();
            var url = $"/webapi/v2/coreServerData/createRemoteConnect/{key}/{server.ServerGuid}";

            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(url, new { targetSubDomain = "www" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(url, new { targetSubDomain = "a.b" })).StatusCode);

            var ok = await client.PostAsJsonAsync(url, new { targetSubDomain = "MyHome" });
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            var body = await ok.Content.ReadFromJsonAsync<JsonElement>(CloudFactory.Json);
            Assert.Equal("myhome", body.GetProperty("subDomain").GetString());
            Assert.Equal(CloudFactory.FrpsToken, body.GetProperty("frpsToken").GetString());
        }

        [Fact]
        public async Task Frps_plugin_allows_only_assigned_subdomains()
        {
            var owner = await Owner("frps-owner");
            var (server, key) = await _factory.CreateServerAsync(owner.ObjId);
            var client = _factory.Client();
            await client.PostAsJsonAsync($"/webapi/v2/coreServerData/createRemoteConnect/{key}/{server.ServerGuid}", new { targetSubDomain = "frpshome" });

            async Task<JsonElement> Call(string secret, string op, object content)
            {
                var json = JsonSerializer.Serialize(new { version = "0.1.0", op, content });
                var response = await client.PostAsync($"/webapi/v1/frps/handler/{secret}?op={op}", new StringContent(json, Encoding.UTF8, "application/json"));
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return default;
                }
                return await response.Content.ReadFromJsonAsync<JsonElement>(CloudFactory.Json);
            }

            Assert.Equal(JsonValueKind.Undefined, (await Call("wrong-secret", "Login", new { user = server.ServerGuid.ToString() })).ValueKind);

            Assert.False((await Call(CloudFactory.FrpsSecret, "Login", new { user = server.ServerGuid.ToString() })).GetProperty("reject").GetBoolean());
            Assert.True((await Call(CloudFactory.FrpsSecret, "Login", new { user = Guid.NewGuid().ToString() })).GetProperty("reject").GetBoolean());
            Assert.True((await Call(CloudFactory.FrpsSecret, "Login", new { user = server.ServerGuid.ToString(), metas = new { apikey = Guid.NewGuid().ToString() } })).GetProperty("reject").GetBoolean());
            Assert.False((await Call(CloudFactory.FrpsSecret, "Login", new { user = server.ServerGuid.ToString(), metas = new { apikey = key.ToString() } })).GetProperty("reject").GetBoolean());

            var user = new { user = server.ServerGuid.ToString() };
            Assert.False((await Call(CloudFactory.FrpsSecret, "NewProxy", new { user, proxy_type = "http", subdomain = "frpshome" })).GetProperty("reject").GetBoolean());
            Assert.True((await Call(CloudFactory.FrpsSecret, "NewProxy", new { user, proxy_type = "http", subdomain = "someone-else" })).GetProperty("reject").GetBoolean());
            Assert.True((await Call(CloudFactory.FrpsSecret, "NewProxy", new { user, proxy_type = "http", subdomain = "frpshome", custom_domains = new[] { "evil.example" } })).GetProperty("reject").GetBoolean());
            Assert.True((await Call(CloudFactory.FrpsSecret, "NewProxy", new { user, proxy_type = "tcp", remote_port = 22 })).GetProperty("reject").GetBoolean());
        }

        [Fact]
        public async Task Tts_validates_input()
        {
            var owner = await Owner("tts-owner");
            var (server, key) = await _factory.CreateServerAsync(owner.ObjId);
            var url = $"/webapi/v2/tts/{key}/{server.ServerGuid}";
            var client = _factory.Client();

            var ok = await client.PostAsJsonAsync(url, new { id = Guid.NewGuid(), text = "Hallo", language = "de-DE" });
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(url, new { id = Guid.NewGuid(), text = new string('a', 2001), language = "de-DE" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(url, new { id = Guid.NewGuid(), text = "x", language = "de-DE\"/><x" })).StatusCode);
        }
    }
}
