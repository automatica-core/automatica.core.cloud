using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.WebApi.Authentication;
using Automatica.Core.Cloud.WebApi.Dtos;
using Automatica.Core.Cloud.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    [ApiController]
    [Route("webapi/v{version:apiVersion}/plugins"), ApiVersion("1.0")]
    [Authorize(Policy = Policies.SysAdmin)]
    public class PluginsController : BaseController
    {
        private readonly CoreContext _db;
        private readonly PackageService _packages;

        public PluginsController(CoreContext db, PackageService packages)
        {
            _db = db;
            _packages = packages;
        }

        [HttpGet("")]
        public async Task<IList<PluginDto>> GetPlugins()
        {
            var plugins = await _db.Plugins.AsNoTracking().Include(a => a.LicenseFeatures).ToListAsync();
            return plugins.OrderBy(a => a.Name).ThenByDescending(a => a.VersionObj).Select(PluginDto.From).ToList();
        }

        [HttpPost("upload"), DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = 1L << 30)]
        public async Task<ActionResult<PluginDto>> Upload([FromForm] IFormFile file, [FromForm] string branch = "develop")
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "no-file" });
            }
            var user = await _db.Users.SingleAsync(a => a.ObjId == CurrentUserId);
            var plugin = await _packages.DeployPluginAsync(file, user, branch, false, HttpContext.RequestAborted);
            return PluginDto.From(plugin);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<PluginDto>> Update(Guid id, [FromBody] PluginUpdateDto data)
        {
            var plugin = await _db.Plugins.Include(a => a.LicenseFeatures).SingleOrDefaultAsync(a => a.ObjId == id);
            if (plugin == null)
            {
                return NotFound();
            }
            plugin.IsPublic = data.IsPublic;
            plugin.IsPrerelease = data.IsPrerelease;
            await _db.SaveChangesAsync();
            return PluginDto.From(plugin);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var plugin = await _db.Plugins.SingleOrDefaultAsync(a => a.ObjId == id);
            if (plugin == null)
            {
                return NotFound();
            }
            await _packages.DeletePluginAsync(plugin, HttpContext.RequestAborted);
            return NoContent();
        }
    }

    [ApiController]
    [Route("webapi/v{version:apiVersion}/coreServerVersion"), ApiVersion("1.0")]
    [Authorize(Policy = Policies.SysAdmin)]
    public class CoreServerVersionsController : BaseController
    {
        private readonly CoreContext _db;
        private readonly PackageService _packages;
        private readonly IPackageStore _store;

        public CoreServerVersionsController(CoreContext db, PackageService packages, IPackageStore store)
        {
            _db = db;
            _packages = packages;
            _store = store;
        }

        [HttpGet("")]
        public async Task<IList<ServerVersionDto>> GetVersions()
        {
            var versions = await _db.Versions.AsNoTracking().ToListAsync();
            return versions.OrderByDescending(a => a.VersionObj).ThenBy(a => a.Rid).Select(ServerVersionDto.From).ToList();
        }

        [HttpPost("upload"), DisableRequestSizeLimit, RequestFormLimits(MultipartBodyLengthLimit = 1L << 30)]
        public async Task<ActionResult<ServerVersionDto>> Upload([FromForm] IFormFile file, [FromForm] string branch = "develop")
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "no-file" });
            }
            var version = await _packages.DeployUpdateAsync(file, branch, HttpContext.RequestAborted);
            return ServerVersionDto.From(version);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ServerVersionDto>> Update(Guid id, [FromBody] ServerVersionUpdateDto data)
        {
            var version = await _db.Versions.SingleOrDefaultAsync(a => a.ObjId == id);
            if (version == null)
            {
                return NotFound();
            }
            version.ChangeLog = data.ChangeLog;
            version.IsPreRelease = data.IsPreRelease;
            version.IsPublic = data.IsPublic;
            await _db.SaveChangesAsync();
            return ServerVersionDto.From(version);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var version = await _db.Versions.SingleOrDefaultAsync(a => a.ObjId == id);
            if (version == null)
            {
                return NotFound();
            }
            await _store.DeleteAsync(version.AzureFileName, HttpContext.RequestAborted);
            _db.Versions.Remove(version);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }

    [ApiController]
    [Route("webapi/v{version:apiVersion}/coreServerDockerVersion"), ApiVersion("1.0")]
    [Authorize(Policy = Policies.SysAdmin)]
    public class CoreServerDockerVersionsController : BaseController
    {
        private readonly CoreContext _db;

        public CoreServerDockerVersionsController(CoreContext db)
        {
            _db = db;
        }

        [HttpGet("")]
        public async Task<IList<DockerVersionDto>> GetVersions()
        {
            var versions = await _db.DockerVersions.AsNoTracking().ToListAsync();
            return versions.OrderByDescending(a => a.VersionObj).Select(DockerVersionDto.From).ToList();
        }

        [HttpPost("")]
        public async Task<ActionResult<DockerVersionDto>> Create([FromBody] DockerVersionDto data)
        {
            var entity = new ServerDockerVersion { ObjId = Guid.NewGuid() };
            Apply(entity, data);
            _db.DockerVersions.Add(entity);
            await _db.SaveChangesAsync();
            return DockerVersionDto.From(entity);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<DockerVersionDto>> Update(Guid id, [FromBody] DockerVersionDto data)
        {
            var entity = await _db.DockerVersions.SingleOrDefaultAsync(a => a.ObjId == id);
            if (entity == null)
            {
                return NotFound();
            }
            Apply(entity, data);
            await _db.SaveChangesAsync();
            return DockerVersionDto.From(entity);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _db.DockerVersions.SingleOrDefaultAsync(a => a.ObjId == id);
            if (entity == null)
            {
                return NotFound();
            }
            _db.DockerVersions.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private static void Apply(ServerDockerVersion entity, DockerVersionDto data)
        {
            entity.Version = data.Version;
            entity.ImageName = data.ImageName;
            entity.ImageTag = data.ImageTag;
            entity.Branch = data.Branch;
            entity.ChangeLog = data.ChangeLog;
            entity.IsPreRelease = data.IsPreRelease;
            entity.IsPublic = data.IsPublic;
        }
    }

    [ApiController]
    [Route("webapi/v{version:apiVersion}/deployKeys"), ApiVersion("1.0")]
    [Authorize(Policy = Policies.SysAdmin)]
    public class DeployKeysController : BaseController
    {
        private readonly CoreContext _db;

        public DeployKeysController(CoreContext db)
        {
            _db = db;
        }

        [HttpGet("")]
        public async Task<IList<DeployKeyDto>> GetKeys()
        {
            var keys = await _db.DeployKeys.AsNoTracking().Include(a => a.This2UserNavigation).OrderByDescending(a => a.CreatedAt).ToListAsync();
            return keys.Select(DeployKeyDto.From).ToList();
        }

        [HttpPost("")]
        public async Task<ActionResult<DeployKeyCreatedDto>> Create([FromBody] DeployKeyCreateDto data)
        {
            var scopes = DeployKeyScope.None;
            foreach (var scope in data.Scopes)
            {
                if (!Enum.TryParse<DeployKeyScope>(scope, out var parsed) || parsed == DeployKeyScope.None)
                {
                    return BadRequest(new { error = "invalid-scope", scope });
                }
                scopes |= parsed;
            }

            var publisherId = data.PublisherUserId ?? CurrentUserId;
            var publisher = await _db.Users.SingleOrDefaultAsync(a => a.ObjId == publisherId);
            if (publisher == null)
            {
                return BadRequest(new { error = "publisher-not-found" });
            }

            var raw = SecretHashing.NewToken(DeployKeyFilter.KeyPrefix, 32);
            var key = new DeployKey
            {
                ObjId = Guid.NewGuid(),
                Name = data.Name,
                KeyHash = SecretHashing.Sha256(raw),
                KeyPrefix = raw[..12],
                Scopes = scopes,
                AllowedBranches = data.AllowedBranches,
                This2User = publisher.ObjId,
                This2UserNavigation = publisher,
                CreatedBy = CurrentUserId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = data.ExpiresInDays.HasValue ? DateTime.UtcNow.AddDays(data.ExpiresInDays.Value) : null
            };
            _db.DeployKeys.Add(key);
            await _db.SaveChangesAsync();

            return new DeployKeyCreatedDto { DeployKey = DeployKeyDto.From(key), Key = raw };
        }

        [HttpPost("{id:guid}/revoke")]
        public async Task<ActionResult<DeployKeyDto>> Revoke(Guid id)
        {
            var key = await _db.DeployKeys.Include(a => a.This2UserNavigation).SingleOrDefaultAsync(a => a.ObjId == id);
            if (key == null)
            {
                return NotFound();
            }
            key.RevokedAt ??= DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return DeployKeyDto.From(key);
        }
    }
}
