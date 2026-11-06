using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Common.Update;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Plugin = Automatica.Core.Cloud.EF.Models.Plugin;

namespace Automatica.Core.Cloud.WebApi.Services
{
    /// <summary>
    /// Validates uploaded plugin/update packages (manifest) and stores them. Every upload gets its own
    /// random temp directory, so concurrent uploads with the same file name cannot overwrite each other.
    /// </summary>
    public partial class PackageService
    {
        private readonly CoreContext _db;
        private readonly IPackageStore _store;
        private readonly ILogger<PackageService> _logger;

        public PackageService(CoreContext db, IPackageStore store, ILogger<PackageService> logger)
        {
            _db = db;
            _store = store;
            _logger = logger;
        }

        [GeneratedRegex("^[A-Za-z0-9._-]{1,100}$")]
        private static partial Regex SafeSegment();

        public static bool IsValidBranch(string branch)
        {
            return !string.IsNullOrEmpty(branch) && SafeSegment().IsMatch(branch);
        }

        private static string SafeFileName(IFormFile file)
        {
            var name = Path.GetFileName(file.FileName ?? "package.zip");
            name = Regex.Replace(name, "[^A-Za-z0-9._-]", "_");
            return string.IsNullOrEmpty(name) ? "package.zip" : name;
        }

        private sealed class TempPackage : IDisposable
        {
            public TempPackage(string root, string file)
            {
                Root = root;
                File = file;
            }

            public string Root { get; }
            public string File { get; }
            public string ExtractPath => Path.Combine(Root, "x");

            public void Dispose()
            {
                try
                {
                    if (Directory.Exists(Root))
                    {
                        Directory.Delete(Root, true);
                    }
                }
                catch (IOException)
                {
                    // temp cleanup is best effort
                }
            }
        }

        private static async Task<TempPackage> SaveTempAsync(IFormFile file, CancellationToken token)
        {
            var root = Path.Combine(Path.GetTempPath(), "acc-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, SafeFileName(file));
            await using (var stream = System.IO.File.Create(path))
            {
                await file.CopyToAsync(stream, token);
            }
            return new TempPackage(root, path);
        }

        public async Task<Plugin> DeployPluginAsync(IFormFile file, User publisher, string branch, bool deleteOldPackages, CancellationToken token = default)
        {
            if (!IsValidBranch(branch))
            {
                throw new ArgumentException("Invalid branch");
            }

            using var temp = await SaveTempAsync(file, token);
            var manifest = Common.Update.Plugin.GetPluginManifest(_logger, temp.File, temp.ExtractPath);
            if (manifest?.Automatica == null)
            {
                throw new ArgumentException("Invalid plugin package");
            }
            if (!Common.Update.Plugin.CheckPluginFile(_logger, temp.File, false))
            {
                throw new ArgumentException("Invalid plugin package");
            }

            var data = manifest.Automatica;
            var plugin = (await _db.Plugins.Include(a => a.LicenseFeatures).ToListAsync(token))
                .SingleOrDefault(a => a.VersionObj == data.PluginVersion && a.Name == data.Name && a.Branch == branch);
            var isNew = plugin == null;

            if (isNew)
            {
                plugin = new Plugin
                {
                    Branch = branch,
                    IsPublic = false,
                    Name = data.Name,
                    This2User = publisher.ObjId,
                    Publisher = publisher.PublisherName,
                    PluginGuid = data.PluginGuid
                };
                foreach (var feature in data.MandatoryLicenseFeatures ?? new List<string>())
                {
                    plugin.LicenseFeatures.Add(new PluginFeature { FeatureName = feature, IsMandatory = true, This2PluginNavigation = plugin });
                }
                foreach (var feature in data.OptionalLicenseFeatures ?? new List<string>())
                {
                    plugin.LicenseFeatures.Add(new PluginFeature { FeatureName = feature, IsMandatory = false, This2PluginNavigation = plugin });
                }
            }

            plugin.Version = data.PluginVersion.ToString();
            plugin.PluginVersion = data.PluginVersion.ToString();
            plugin.MinCoreServerVersion = data.MinCoreServerVersion?.ToString();
            plugin.PluginType = data.Type == "driver" ? PluginType.Driver : PluginType.Rule;
            plugin.ComponentName = data.ComponentName;

            var blobName = $"{data.PluginVersion}-{branch}-{SafeFileName(file)}";
            await using (var stream = System.IO.File.OpenRead(temp.File))
            {
                var uri = await _store.UploadAsync(blobName, stream, new Dictionary<string, string>
                {
                    { "version", data.PluginVersion.ToString() },
                    { "branch", branch }
                }, token);
                plugin.AzureUrl = uri.ToString();
                plugin.AzureFileName = blobName;
            }

            if (isNew)
            {
                _db.Plugins.Add(plugin);
            }
            await _db.SaveChangesAsync(token);

            if (deleteOldPackages)
            {
                // delete only older packages with the same min core server version to stay compatible
                var others = (await _db.Plugins.ToListAsync(token))
                    .Where(a => a.ObjId != plugin.ObjId && a.Name == plugin.Name && a.Branch == branch &&
                                a.VersionObj < plugin.VersionObj && a.MinCoreServerVersionObj == plugin.MinCoreServerVersionObj)
                    .ToList();
                foreach (var other in others)
                {
                    await DeletePluginAsync(other, token);
                }
            }

            _logger.LogInformation("Deployed plugin {Name} {Version} to {Branch} as {Publisher}", plugin.Name, plugin.Version, branch, publisher.UserName);
            return plugin;
        }

        public async Task DeletePluginAsync(Plugin plugin, CancellationToken token = default)
        {
            await _store.DeleteAsync(plugin.AzureFileName, token);
            var features = await _db.PluginFeatures.Where(a => a.This2Plugin == plugin.ObjId).ToListAsync(token);
            _db.PluginFeatures.RemoveRange(features);
            _db.Plugins.Remove(plugin);
            await _db.SaveChangesAsync(token);
        }

        public async Task<EF.Models.ServerVersion> DeployUpdateAsync(IFormFile file, string branch, CancellationToken token = default)
        {
            if (!IsValidBranch(branch))
            {
                throw new ArgumentException("Invalid branch");
            }

            using var temp = await SaveTempAsync(file, token);
            var manifest = Update.GetUpdateManifest(_logger, temp.File, temp.ExtractPath);
            if (manifest == null || string.IsNullOrEmpty(manifest.Rid))
            {
                throw new ArgumentException("Invalid update package");
            }
            if (!Update.CheckUpdateFile(_logger, temp.File, manifest.Rid))
            {
                throw new ArgumentException("Invalid update package");
            }

            var version = (await _db.Versions.ToListAsync(token))
                .SingleOrDefault(a => a.VersionObj == manifest.Version && a.Rid == manifest.Rid && a.Branch == branch);
            var isNew = version == null;
            if (isNew)
            {
                version = new EF.Models.ServerVersion
                {
                    IsPreRelease = manifest.PreRelease,
                    Rid = manifest.Rid,
                    ChangeLog = "",
                    Version = manifest.Version.ToString(),
                    Branch = branch
                };
            }

            var blobName = $"{version.Rid}_{version.Version}-{SafeFileName(file)}";
            await using (var stream = System.IO.File.OpenRead(temp.File))
            {
                var uri = await _store.UploadAsync(blobName, stream, new Dictionary<string, string>
                {
                    { "version", version.Version },
                    { "rid", version.Rid }
                }, token);
                version.AzureUrl = uri.ToString();
                version.AzureFileName = blobName;
            }

            if (isNew)
            {
                _db.Versions.Add(version);
            }
            await _db.SaveChangesAsync(token);
            _logger.LogInformation("Deployed update {Version} ({Rid}) to {Branch}", version.Version, version.Rid, branch);
            return version;
        }
    }
}
