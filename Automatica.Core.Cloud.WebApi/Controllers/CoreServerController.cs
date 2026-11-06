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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    /// <summary>
    /// Core servers of the signed-in user. Administrators see and manage all servers.
    /// The api key is returned only by create and rotate; it is stored as a hash.
    /// </summary>
    [ApiController]
    [Route("webapi/v{version:apiVersion}/coreServer"), ApiVersion("1.0")]
    public class CoreServerController : BaseController
    {
        private readonly CoreContext _db;
        private readonly IMailSender _mail;
        private readonly IConfiguration _config;
        private readonly ILogger<CoreServerController> _logger;

        public CoreServerController(CoreContext db, IMailSender mail, IConfiguration config, ILogger<CoreServerController> logger)
        {
            _db = db;
            _mail = mail;
            _config = config;
            _logger = logger;
        }

        private IQueryable<CoreServer> Visible()
        {
            var query = _db.CoreServers.Include(a => a.This2UserNavigation).AsQueryable();
            return IsAdmin ? query : query.Where(a => a.This2User == CurrentUserId);
        }

        private async Task<CoreServerDto> ToDto(CoreServer server)
        {
            var license = await _db.Licenses.AsNoTracking().Where(a => a.This2CoreServer == server.ObjId)
                .OrderByDescending(a => a.ExpiresAt).FirstOrDefaultAsync();
            return CoreServerDto.From(server, license);
        }

        [HttpGet("")]
        public async Task<IList<CoreServerDto>> GetServers([FromQuery] bool mine = false)
        {
            var query = Visible().AsNoTracking();
            if (mine)
            {
                query = query.Where(a => a.This2User == CurrentUserId);
            }
            var servers = await query.OrderBy(a => a.ServerName).ToListAsync();
            var ids = servers.Select(a => a.ObjId).ToList();
            var licenses = (await _db.Licenses.AsNoTracking().Where(a => ids.Contains(a.This2CoreServer)).ToListAsync())
                .GroupBy(a => a.This2CoreServer)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.ExpiresAt).First());

            return servers.Select(s => CoreServerDto.From(s, licenses.GetValueOrDefault(s.ObjId))).ToList();
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<CoreServerDto>> Get(Guid id)
        {
            var server = await Visible().AsNoTracking().SingleOrDefaultAsync(a => a.ObjId == id);
            return server == null ? NotFound() : await ToDto(server);
        }

        [HttpPost("")]
        public async Task<ActionResult<CoreServerSecretDto>> Create([FromBody] CoreServerSaveDto data)
        {
            var owner = CurrentUserId;
            if (data.OwnerId.HasValue && data.OwnerId.Value != owner)
            {
                if (!IsAdmin)
                {
                    return Forbid();
                }
                if (!await _db.Users.AnyAsync(a => a.ObjId == data.OwnerId.Value))
                {
                    return BadRequest(new { error = "owner-not-found" });
                }
                owner = data.OwnerId.Value;
            }

            var serverGuid = data.ServerGuid is { } g && g != Guid.Empty ? g : Guid.NewGuid();
            if (await _db.CoreServers.AnyAsync(a => a.ServerGuid == serverGuid))
            {
                return Conflict(new { error = "server-guid-exists" });
            }

            var apiKey = Guid.NewGuid();
            var server = new CoreServer
            {
                ObjId = Guid.NewGuid(),
                ServerGuid = serverGuid,
                ServerName = data.ServerName,
                This2User = owner,
                ApiKey = Guid.Empty,
                ApiKeyHash = SecretHashing.HashApiKey(apiKey),
                ApiKeyCreatedAt = DateTime.UtcNow,
                Version = "0.0.0.0"
            };
            _db.CoreServers.Add(server);
            await _db.SaveChangesAsync();
            await _db.Entry(server).Reference(a => a.This2UserNavigation).LoadAsync();

            return new CoreServerSecretDto { Server = await ToDto(server), ApiKey = apiKey };
        }

        /// <summary>Only the name (and, for admins, the owner) can be changed; nothing else is bound from the body.</summary>
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<CoreServerDto>> Update(Guid id, [FromBody] CoreServerSaveDto data)
        {
            var server = await Visible().SingleOrDefaultAsync(a => a.ObjId == id);
            if (server == null)
            {
                return NotFound();
            }

            server.ServerName = data.ServerName;
            if (data.OwnerId.HasValue && data.OwnerId.Value != server.This2User)
            {
                if (!IsAdmin)
                {
                    return Forbid();
                }
                if (!await _db.Users.AnyAsync(a => a.ObjId == data.OwnerId.Value))
                {
                    return BadRequest(new { error = "owner-not-found" });
                }
                server.This2User = data.OwnerId.Value;
                server.This2UserNavigation = null;
            }
            await _db.SaveChangesAsync();
            await _db.Entry(server).Reference(a => a.This2UserNavigation).LoadAsync();
            return await ToDto(server);
        }

        [HttpPost("{id:guid}/rotateKey")]
        public async Task<ActionResult<CoreServerSecretDto>> RotateKey(Guid id)
        {
            var server = await Visible().SingleOrDefaultAsync(a => a.ObjId == id);
            if (server == null)
            {
                return NotFound();
            }

            var apiKey = Guid.NewGuid();
            server.ApiKey = Guid.Empty;
            server.ApiKeyHash = SecretHashing.HashApiKey(apiKey);
            server.ApiKeyCreatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Rotated api key of core server {ServerId}", server.ObjId);

            return new CoreServerSecretDto { Server = await ToDto(server), ApiKey = apiKey };
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var server = await Visible().SingleOrDefaultAsync(a => a.ObjId == id);
            if (server == null)
            {
                return NotFound();
            }

            _db.Licenses.RemoveRange(await _db.Licenses.Where(a => a.This2CoreServer == id).ToListAsync());
            _db.RemoteControlPorts.RemoveRange(await _db.RemoteControlPorts.Where(a => a.This2CoreServer == id).ToListAsync());
            _db.RemoteControlSubDomains.RemoveRange(await _db.RemoteControlSubDomains.Where(a => a.This2CoreServer == id).ToListAsync());
            _db.MailRecipients.RemoveRange(await _db.MailRecipients.Where(a => a.This2CoreServer == id).ToListAsync());
            _db.CoreServers.Remove(server);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ---- mail recipients (M3) ----

        [HttpGet("{id:guid}/mailRecipients")]
        public async Task<ActionResult<IList<MailRecipientDto>>> GetMailRecipients(Guid id)
        {
            var server = await Visible().AsNoTracking().SingleOrDefaultAsync(a => a.ObjId == id);
            if (server == null)
            {
                return NotFound();
            }

            var list = new List<MailRecipientDto>();
            if (!string.IsNullOrEmpty(server.This2UserNavigation?.Email))
            {
                list.Add(new MailRecipientDto { Email = server.This2UserNavigation.Email, Verified = true, IsOwner = true });
            }
            var recipients = await _db.MailRecipients.AsNoTracking().Where(a => a.This2CoreServer == id).OrderBy(a => a.Email).ToListAsync();
            list.AddRange(recipients.Select(MailRecipientDto.From));
            return list;
        }

        [HttpPost("{id:guid}/mailRecipients"), EnableRateLimiting(RateLimits.Public)]
        public async Task<ActionResult<MailRecipientDto>> AddMailRecipient(Guid id, [FromBody] MailRecipientCreateDto data)
        {
            var server = await Visible().SingleOrDefaultAsync(a => a.ObjId == id);
            if (server == null)
            {
                return NotFound();
            }

            var email = data.Email.Trim();
            if (await _db.MailRecipients.CountAsync(a => a.This2CoreServer == id) >= 20)
            {
                return BadRequest(new { error = "too-many-recipients" });
            }
            var existing = await _db.MailRecipients.SingleOrDefaultAsync(a => a.This2CoreServer == id && a.Email == email);
            if (existing != null)
            {
                return Conflict(new { error = "recipient-exists" });
            }

            var recipient = new MailRecipient
            {
                ObjId = Guid.NewGuid(),
                This2CoreServer = id,
                Email = email,
                CreatedAt = DateTime.UtcNow
            };
            _db.MailRecipients.Add(recipient);
            await SendVerificationAsync(server, recipient);
            await _db.SaveChangesAsync();
            return MailRecipientDto.From(recipient);
        }

        [HttpPost("{id:guid}/mailRecipients/{recipientId:guid}/resend"), EnableRateLimiting(RateLimits.Public)]
        public async Task<IActionResult> ResendVerification(Guid id, Guid recipientId)
        {
            var server = await Visible().SingleOrDefaultAsync(a => a.ObjId == id);
            var recipient = server == null ? null : await _db.MailRecipients.SingleOrDefaultAsync(a => a.ObjId == recipientId && a.This2CoreServer == id);
            if (recipient == null)
            {
                return NotFound();
            }
            if (recipient.VerifiedAt.HasValue)
            {
                return NoContent();
            }
            await SendVerificationAsync(server, recipient);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:guid}/mailRecipients/{recipientId:guid}")]
        public async Task<IActionResult> DeleteMailRecipient(Guid id, Guid recipientId)
        {
            var server = await Visible().AsNoTracking().SingleOrDefaultAsync(a => a.ObjId == id);
            var recipient = server == null ? null : await _db.MailRecipients.SingleOrDefaultAsync(a => a.ObjId == recipientId && a.This2CoreServer == id);
            if (recipient == null)
            {
                return NotFound();
            }
            _db.MailRecipients.Remove(recipient);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        private async Task SendVerificationAsync(CoreServer server, MailRecipient recipient)
        {
            var token = SecretHashing.NewToken("", 32);
            recipient.VerificationTokenHash = SecretHashing.Sha256(token);
            recipient.VerificationExpiresAt = DateTime.UtcNow.AddHours(48);

            // never derived from the Host header, so a forged host cannot end up in the mail
            var baseUrl = _config["App:PublicBaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new InvalidOperationException("App:PublicBaseUrl is not configured");
            }
            var link = $"{baseUrl}/verify-email?token={Uri.EscapeDataString(token)}";
            await _mail.SendAsync(recipient.Email, "Confirm notifications from your Automatica.Core server",
                $"Please confirm that the Automatica.Core server \"{server.ServerName}\" may send notifications to this address:\n\n{link}\n\nThe link expires in 48 hours.",
                MailTemplates.VerificationHtml(server.ServerName, link));
        }
    }

    [ApiController]
    [Route("webapi/v{version:apiVersion}/mail"), ApiVersion("1.0")]
    public class MailVerificationController : BaseController
    {
        private readonly CoreContext _db;

        public MailVerificationController(CoreContext db)
        {
            _db = db;
        }

        [HttpPost("verify"), AllowAnonymous, EnableRateLimiting(RateLimits.Public)]
        public async Task<IActionResult> Verify([FromBody] VerifyMailDto data)
        {
            var hash = SecretHashing.Sha256(data.Token);
            var recipient = (await _db.MailRecipients.Where(a => a.VerificationTokenHash == hash).ToListAsync())
                .FirstOrDefault(a => SecretHashing.FixedTimeEquals(a.VerificationTokenHash, hash));

            if (recipient == null || recipient.VerificationExpiresAt < DateTime.UtcNow)
            {
                return BadRequest(new { error = "invalid-or-expired" });
            }

            recipient.VerifiedAt = DateTime.UtcNow;
            recipient.VerificationTokenHash = null;
            recipient.VerificationExpiresAt = null;
            await _db.SaveChangesAsync();
            return Ok(new { email = recipient.Email });
        }
    }
}
