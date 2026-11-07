using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Automatica.Core.Cloud.EF.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Automatica.Core.Cloud.WebApi.Dtos
{
    // DTOs of the admin web API. None of them carries a password hash, salt, api key or refresh token,
    // except the explicit "...Created" responses that return a freshly generated secret exactly once.

    public class UserDto
    {
        public Guid ObjId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PublisherName { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public UserRole Role { get; set; }
        public bool Enabled { get; set; }
        public bool HasPassword { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LockedUntil { get; set; }

        public static UserDto From(User user) => new()
        {
            ObjId = user.ObjId,
            UserName = user.UserName,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PublisherName = user.PublisherName,
            Role = user.UserRole,
            Enabled = user.Enabled,
            HasPassword = user.HasUsablePassword,
            LastLoginAt = user.LastLoginAt,
            LockedUntil = user.LockedUntil
        };
    }

    public class UserSaveDto
    {
        [Required, StringLength(64, MinimumLength = 2), RegularExpression("^[A-Za-z0-9._@-]+$")]
        public string UserName { get; set; }
        [EmailAddress, StringLength(254)]
        public string Email { get; set; }
        [StringLength(100)]
        public string FirstName { get; set; }
        [StringLength(100)]
        public string LastName { get; set; }
        [StringLength(100)]
        public string PublisherName { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public UserRole Role { get; set; } = UserRole.Viewer;
        public bool Enabled { get; set; } = true;
        /// <summary>Only on create; optional, a random one is generated and returned once otherwise.</summary>
        [StringLength(256)]
        public string Password { get; set; }
    }

    public class UserCreatedDto
    {
        public UserDto User { get; set; }
        /// <summary>Only set when the password was generated. Shown once.</summary>
        public string InitialPassword { get; set; }
    }

    public class PasswordDto
    {
        [StringLength(256)]
        public string CurrentPassword { get; set; }
        [Required, StringLength(256)]
        public string NewPassword { get; set; }
    }

    public class LicenseSummaryDto
    {
        public Guid ObjId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string LicensedTo { get; set; }
        public bool AllowRemoteControl { get; set; }
        public bool AllowTextToSpeech { get; set; }
        public int MaxDatapoints { get; set; }
        public int MaxUsers { get; set; }
    }

    public class CoreServerDto
    {
        public Guid ObjId { get; set; }
        public Guid ServerGuid { get; set; }
        public string ServerName { get; set; }
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; }
        public string Version { get; set; }
        public string Rid { get; set; }
        public DateTime? LastKnownConnection { get; set; }
        public string LastKnownRemoteConnectUrl { get; set; }
        public DateTime? LastKnownRemoteConnectUrlDate { get; set; }
        public DateTime? ApiKeyCreatedAt { get; set; }
        public bool HasApiKey { get; set; }
        public LicenseSummaryDto License { get; set; }

        public static CoreServerDto From(CoreServer server, License license = null) => new()
        {
            ObjId = server.ObjId,
            ServerGuid = server.ServerGuid,
            ServerName = server.ServerName,
            OwnerId = server.This2User,
            OwnerName = server.This2UserNavigation?.UserName,
            Version = server.Version,
            Rid = server.Rid,
            LastKnownConnection = server.LastKnownConnection,
            LastKnownRemoteConnectUrl = server.LastKnownRemoteConnectUrl,
            LastKnownRemoteConnectUrlDate = server.LastKnownRemoteConnectUrlDate,
            ApiKeyCreatedAt = server.ApiKeyCreatedAt,
            HasApiKey = server.ApiKeyHash != null,
            License = license == null ? null : new LicenseSummaryDto
            {
                ObjId = license.ObjId,
                ExpiresAt = license.ExpiresAt,
                LicensedTo = license.LicensedTo,
                AllowRemoteControl = license.AllowRemoteControl,
                AllowTextToSpeech = license.AllowTextToSpeech,
                MaxDatapoints = license.MaxDatapoints,
                MaxUsers = license.MaxUsers
            }
        };
    }

    public class CoreServerSaveDto
    {
        [Required, StringLength(100, MinimumLength = 1)]
        public string ServerName { get; set; }
        /// <summary>Only on create. Empty = a new one is generated.</summary>
        public Guid? ServerGuid { get; set; }
        /// <summary>Admins only: assign the server to another user.</summary>
        public Guid? OwnerId { get; set; }
    }

    public class CoreServerSecretDto
    {
        public CoreServerDto Server { get; set; }
        /// <summary>The api key, returned only by create and rotate. It is stored hashed and cannot be shown again.</summary>
        public Guid ApiKey { get; set; }
    }

    public class MailRecipientDto
    {
        public Guid ObjId { get; set; }
        public string Email { get; set; }
        public bool Verified { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsOwner { get; set; }

        public static MailRecipientDto From(MailRecipient r) => new()
        {
            ObjId = r.ObjId,
            Email = r.Email,
            Verified = r.VerifiedAt.HasValue,
            VerifiedAt = r.VerifiedAt,
            CreatedAt = r.CreatedAt
        };
    }

    public class MailRecipientCreateDto
    {
        [Required, EmailAddress, StringLength(254)]
        public string Email { get; set; }
    }

    public class VerifyMailDto
    {
        [Required, StringLength(200)]
        public string Token { get; set; }
    }

    public class LicenseDto
    {
        public Guid ObjId { get; set; }
        public Guid CoreServerId { get; set; }
        public string ServerName { get; set; }
        public Guid? ServerGuid { get; set; }
        public string OwnerName { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int MaxDatapoints { get; set; }
        public int MaxUsers { get; set; }
        public string LicensedTo { get; set; }
        public string Email { get; set; }
        public bool AllowRemoteControl { get; set; }
        public int MaxRemoteTunnels { get; set; }
        public long MaxRecordingDataPoints { get; set; }
        public int MaxSatellites { get; set; }
        public bool AllowTextToSpeech { get; set; }
        public IList<string> Features { get; set; }

        public static LicenseDto From(License l) => new()
        {
            ObjId = l.ObjId,
            CoreServerId = l.This2CoreServer,
            ServerName = l.This2CoreServerNavigation?.ServerName,
            ServerGuid = l.This2CoreServerNavigation?.ServerGuid,
            OwnerName = l.This2CoreServerNavigation?.This2UserNavigation?.UserName,
            ExpiresAt = l.ExpiresAt,
            MaxDatapoints = l.MaxDatapoints,
            MaxUsers = l.MaxUsers,
            LicensedTo = l.LicensedTo,
            Email = l.Email,
            AllowRemoteControl = l.AllowRemoteControl,
            MaxRemoteTunnels = l.MaxRemoteTunnels,
            MaxRecordingDataPoints = l.MaxRecordingDataPoints,
            MaxSatellites = l.MaxSatellites,
            AllowTextToSpeech = l.AllowTextToSpeech,
            Features = string.IsNullOrEmpty(l.FeaturesString)
                ? new List<string>()
                : l.FeaturesString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
        };
    }

    public class GenerateLicenseDto
    {
        [Required]
        public Guid CoreServerId { get; set; }
        [Required]
        public DateTime ExpiresAt { get; set; }
        [Range(0, int.MaxValue)]
        public int MaxDatapoints { get; set; }
        [Range(0, int.MaxValue)]
        public int MaxUsers { get; set; }
        [Required, StringLength(200)]
        public string LicensedTo { get; set; }
        [EmailAddress, StringLength(254)]
        public string Email { get; set; }
        public bool AllowRemoteControl { get; set; }
        [Range(0, 1000)]
        public int MaxRemoteTunnels { get; set; }
        [Range(0, long.MaxValue)]
        public long MaxRecordingDataPoints { get; set; }
        [Range(0, 1000)]
        public int MaxSatellites { get; set; }
        public bool AllowTextToSpeech { get; set; }
        public IList<string> Features { get; set; } = new List<string>();
    }

    public class PluginDto
    {
        public Guid ObjId { get; set; }
        public Guid PluginGuid { get; set; }
        public string Name { get; set; }
        public string ComponentName { get; set; }
        public string Version { get; set; }
        public string MinCoreServerVersion { get; set; }
        public string Branch { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public PluginType PluginType { get; set; }
        public string Publisher { get; set; }
        public bool IsPublic { get; set; }
        public bool IsPrerelease { get; set; }
        public string AzureUrl { get; set; }
        public IList<string> Features { get; set; }

        public static PluginDto From(Plugin p) => new()
        {
            ObjId = p.ObjId,
            PluginGuid = p.PluginGuid,
            Name = p.Name,
            ComponentName = p.ComponentName,
            Version = p.Version,
            MinCoreServerVersion = p.MinCoreServerVersion,
            Branch = p.Branch,
            PluginType = p.PluginType,
            Publisher = p.Publisher,
            IsPublic = p.IsPublic,
            IsPrerelease = p.IsPrerelease,
            AzureUrl = p.AzureUrl,
            Features = p.LicenseFeatures?.Select(a => a.FeatureName).Distinct().ToList() ?? new List<string>()
        };
    }

    public class PluginUpdateDto
    {
        public bool IsPublic { get; set; }
        public bool IsPrerelease { get; set; }
    }

    public class ServerVersionDto
    {
        public Guid ObjId { get; set; }
        public string Version { get; set; }
        public string Rid { get; set; }
        public string Branch { get; set; }
        public string ChangeLog { get; set; }
        public bool IsPreRelease { get; set; }
        public bool IsPublic { get; set; }
        public string AzureUrl { get; set; }

        public static ServerVersionDto From(ServerVersion v) => new()
        {
            ObjId = v.ObjId,
            Version = v.Version,
            Rid = v.Rid,
            Branch = v.Branch,
            ChangeLog = v.ChangeLog,
            IsPreRelease = v.IsPreRelease,
            IsPublic = v.IsPublic,
            AzureUrl = v.AzureUrl
        };
    }

    public class ServerVersionUpdateDto
    {
        [StringLength(10000)]
        public string ChangeLog { get; set; }
        public bool IsPreRelease { get; set; }
        public bool IsPublic { get; set; }
    }

    public class DockerVersionDto
    {
        public Guid ObjId { get; set; }
        [Required, RegularExpression("^\\d+(\\.\\d+){1,3}$")]
        public string Version { get; set; }
        [Required, StringLength(200), RegularExpression("^[a-z0-9]+([._/-][a-z0-9]+)*$")]
        public string ImageName { get; set; }
        [Required, StringLength(128), RegularExpression("^[A-Za-z0-9_][A-Za-z0-9_.-]{0,127}$")]
        public string ImageTag { get; set; }
        [Required, StringLength(100), RegularExpression("^[A-Za-z0-9._-]+$")]
        public string Branch { get; set; }
        [StringLength(10000)]
        public string ChangeLog { get; set; }
        public bool IsPreRelease { get; set; }
        public bool IsPublic { get; set; }

        public static DockerVersionDto From(ServerDockerVersion v) => new()
        {
            ObjId = v.ObjId,
            Version = v.Version,
            ImageName = v.ImageName,
            ImageTag = v.ImageTag,
            Branch = v.Branch,
            ChangeLog = v.ChangeLog,
            IsPreRelease = v.IsPreRelease,
            IsPublic = v.IsPublic
        };
    }

    public class DeployKeyDto
    {
        public Guid ObjId { get; set; }
        public string Name { get; set; }
        public string KeyPrefix { get; set; }
        public IList<string> Scopes { get; set; }
        public string AllowedBranches { get; set; }
        public Guid PublisherUserId { get; set; }
        public string PublisherUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public string LastUsedIp { get; set; }
        public DateTime? RevokedAt { get; set; }

        public static DeployKeyDto From(DeployKey k) => new()
        {
            ObjId = k.ObjId,
            Name = k.Name,
            KeyPrefix = k.KeyPrefix,
            Scopes = Enum.GetValues<DeployKeyScope>().Where(s => s != DeployKeyScope.None && k.Scopes.HasFlag(s)).Select(s => s.ToString()).ToList(),
            AllowedBranches = k.AllowedBranches,
            PublisherUserId = k.This2User,
            PublisherUserName = k.This2UserNavigation?.UserName,
            CreatedAt = k.CreatedAt,
            ExpiresAt = k.ExpiresAt,
            LastUsedAt = k.LastUsedAt,
            LastUsedIp = k.LastUsedIp,
            RevokedAt = k.RevokedAt
        };
    }

    public class DeployKeyCreateDto
    {
        [Required, StringLength(100, MinimumLength = 2)]
        public string Name { get; set; }
        [Required, MinLength(1)]
        public IList<string> Scopes { get; set; }
        [Required, StringLength(200), RegularExpression("^(\\*|[A-Za-z0-9._-]+)(,(\\*|[A-Za-z0-9._-]+))*$")]
        public string AllowedBranches { get; set; }
        /// <summary>The user the plugins are published as; default = the creating admin.</summary>
        public Guid? PublisherUserId { get; set; }
        [Range(1, 3650)]
        public int? ExpiresInDays { get; set; } = 365;
    }

    public class DeployKeyCreatedDto
    {
        public DeployKeyDto DeployKey { get; set; }
        /// <summary>Shown once; store it as a secret pipeline variable.</summary>
        public string Key { get; set; }
    }

    public class AuthResponseDto
    {
        public string AccessToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; }
    }

    public class LoginDto
    {
        [Required, StringLength(254)]
        public string UserName { get; set; }
        [Required, StringLength(256)]
        public string Password { get; set; }
    }
}
