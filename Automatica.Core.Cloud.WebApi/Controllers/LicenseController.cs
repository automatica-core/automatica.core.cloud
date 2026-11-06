using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asp.Versioning;
using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.LicenseManager;
using Automatica.Core.Cloud.WebApi.Authentication;
using Automatica.Core.Cloud.WebApi.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    /// <summary>License administration. Every action needs an administrator; nothing here is anonymous.</summary>
    [ApiController]
    [Route("webapi/v{version:apiVersion}/license"), ApiVersion("1.0")]
    [Authorize(Policy = Policies.Admin)]
    public class LicenseController : BaseController
    {
        private readonly CoreContext _db;
        private readonly ILicenseManager _licenseManager;

        public LicenseController(CoreContext db, ILicenseManager licenseManager)
        {
            _db = db;
            _licenseManager = licenseManager;
        }

        [HttpGet("")]
        public async Task<IList<LicenseDto>> GetLicenses()
        {
            var licenses = await _db.Licenses.AsNoTracking()
                .Include(a => a.This2CoreServerNavigation).ThenInclude(a => a.This2UserNavigation)
                .OrderBy(a => a.ExpiresAt)
                .ToListAsync();
            return licenses.Select(LicenseDto.From).ToList();
        }

        [HttpGet("features")]
        public async Task<IList<string>> GetAllPluginFeatures()
        {
            var features = await _db.PluginFeatures.AsNoTracking().Select(a => a.FeatureName).Distinct().ToListAsync();
            return features.OrderBy(a => a).ToList();
        }

        /// <summary>The signed license text as the core receives it.</summary>
        [HttpGet("{id:guid}/key")]
        public async Task<ActionResult<string>> GetLicenseKey(Guid id)
        {
            var license = await _db.Licenses.AsNoTracking().SingleOrDefaultAsync(a => a.ObjId == id);
            return license == null ? NotFound() : Content(license.LicenseKey ?? string.Empty, "text/plain");
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var license = await _db.Licenses.SingleOrDefaultAsync(a => a.ObjId == id);
            if (license == null)
            {
                return NotFound();
            }
            _db.Remove(license);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Signs a new license for a server. An existing license of that server is replaced.</summary>
        [HttpPost("")]
        public async Task<ActionResult<LicenseDto>> GenerateLicense([FromBody] GenerateLicenseDto data)
        {
            var coreServer = await _db.CoreServers.Include(a => a.This2UserNavigation).SingleOrDefaultAsync(a => a.ObjId == data.CoreServerId);
            if (coreServer == null)
            {
                return BadRequest(new { error = "server-not-found" });
            }

            var major = coreServer.VersionObj?.Major ?? 0;
            var key = await _db.LicenseKeys.SingleOrDefaultAsync(a => a.Version == major);
            if (key == null)
            {
                return BadRequest(new { error = "no-signing-key-for-version", version = major });
            }

            var features = (data.Features ?? new List<string>()).Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a.Trim()).Distinct().ToList();
            var signed = _licenseManager.CreateLicense(data.MaxDatapoints, data.MaxUsers, coreServer.ObjId, data.ExpiresAt,
                data.LicensedTo, data.Email, data.AllowRemoteControl, data.MaxRemoteTunnels, data.MaxRecordingDataPoints,
                data.MaxSatellites, data.AllowTextToSpeech, features);

            var old = await _db.Licenses.Where(a => a.This2CoreServer == coreServer.ObjId).ToListAsync();
            _db.Licenses.RemoveRange(old);

            var license = new License
            {
                ObjId = Guid.NewGuid(),
                This2CoreServer = coreServer.ObjId,
                This2VersionKey = key.ObjId,
                LicenseKey = signed,
                MaxDatapoints = data.MaxDatapoints,
                MaxUsers = data.MaxUsers,
                MaxRemoteTunnels = data.MaxRemoteTunnels,
                LicensedTo = data.LicensedTo,
                Email = data.Email,
                FeaturesString = string.Join(",", features),
                AllowRemoteControl = data.AllowRemoteControl,
                MaxRecordingDataPoints = data.MaxRecordingDataPoints,
                MaxSatellites = data.MaxSatellites,
                ExpiresAt = data.ExpiresAt,
                AllowTextToSpeech = data.AllowTextToSpeech,
                This2CoreServerNavigation = coreServer
            };
            _db.Licenses.Add(license);
            await _db.SaveChangesAsync();

            return LicenseDto.From(license);
        }

        /// <summary>Creates the signing key pair for a core major version (if missing) and returns the public key.</summary>
        [HttpPost("signingKey/{major:int}"), Authorize(Policy = Policies.SysAdmin)]
        public ActionResult<object> GenerateSigningKey(int major)
        {
            if (major < 0 || major > 1000)
            {
                return BadRequest();
            }
            var publicKey = _licenseManager.GeneratePublicPrivateKey(new Version(major, 0));
            return new { version = major.ToString(), @public = publicKey };
        }
    }
}
