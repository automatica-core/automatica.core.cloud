using Automatica.Core.Cloud.EF.Models;
using Microsoft.Extensions.Configuration;
using Standard.Licensing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Automatica.Core.Cloud.LicenseManager
{
    public class LicenseManager : ILicenseManager
    {
        /// <summary>
        /// Configuration key of the secret part of the private key passphrase (Key Vault: License--PrivateKeyPassword).
        /// Existing keys in the database are encrypted with the value that used to be hard-coded here, so the
        /// Key Vault secret must hold that same value until the signing keys are rotated.
        /// </summary>
        public const string PasswordConfigKey = "License:PrivateKeyPassword";

        private readonly IConfiguration _config;

        public LicenseManager(CoreContext coreContext, IConfiguration config)
        {
            CoreContext = coreContext;
            _config = config;
        }

        public CoreContext CoreContext { get; }

        private string GetPassphrase(int major)
        {
            var secret = _config[PasswordConfigKey];
            if (string.IsNullOrEmpty(secret))
            {
                throw new InvalidOperationException($"{PasswordConfigKey} is not configured; licenses cannot be signed.");
            }
            // keep the historic derivation (major repeated four times + secret) so existing keys still open
            return $"{major}{major}{major}{major}{secret}";
        }

        public string GeneratePublicPrivateKey(Version version)
        {
            var vers = CoreContext.LicenseKeys.SingleOrDefault(a => a.Version == version.Major);

            if (vers != null)
            {
                return vers.PublicKey;
            }

            var keyGenerator = Standard.Licensing.Security.Cryptography.KeyGenerator.Create();
            var keyPair = keyGenerator.GenerateKeyPair();
            vers = new LicenseKey
            {
                PrivateKey = keyPair.ToEncryptedPrivateKeyString(GetPassphrase(version.Major)),
                PublicKey = keyPair.ToPublicKeyString(),
                Version = version.Major,
                ObjId = Guid.NewGuid()
            };

            CoreContext.Add(vers);
            CoreContext.SaveChanges();
            return vers.PublicKey;
        }

        public string CreateLicense(
            int maxDatapoints,
            int maxUsers,
            Guid this2CoreServer,
            DateTime expires,
            string licensedTo,
            string email,
            bool allowRemoteControl,
            int maxRemoteTunnels,
            long maxRecordingDataPoints,
            int maxSatellites,
            bool allowTextToSpeech,
            IList<string> features)
        {
            return Create(LicenseType.Standard, maxDatapoints, maxUsers, this2CoreServer, expires, licensedTo, email, allowRemoteControl, maxRemoteTunnels, maxRecordingDataPoints, maxSatellites, allowTextToSpeech, features);
        }

        public string CreateTrialLicense(
            int maxDatapoints,
            int maxUsers,
            Guid this2CoreServer,
            DateTime expires,
            string licensedTo,
            string email)
        {
            return Create(LicenseType.Trial, maxDatapoints, maxUsers, this2CoreServer, expires, licensedTo, email, false, 0, 50, 1, false, new List<string>());
        }

        private string Create(
            LicenseType type,
            int maxDatapoints,
            int maxUsers,
            Guid this2CoreServer,
            DateTime expires,
            string licensedTo,
            string email,
            bool allowRemoteControl,
            int maxRemoteTunnels,
            long maxRecordingDataPoints,
            int maxSatellites,
            bool allowTextToSpeech,
            IList<string> features)
        {
            var coreServer = CoreContext.CoreServers.Single(a => a.ObjId == this2CoreServer);
            var version = coreServer.VersionObj ?? new Version(0, 0);
            return Create(type, maxDatapoints, maxUsers, version, coreServer.ServerGuid, expires, licensedTo, email, allowRemoteControl, maxRemoteTunnels, maxRecordingDataPoints, maxSatellites, allowTextToSpeech, features);
        }

        private string Create(
            LicenseType type,
            int maxDatapoints, int maxUsers,
            Version version,
            Guid licenseId,
            DateTime expires,
            string licensedTo,
            string email,
            bool allowRemoteControl,
            int maxRemoteTunnels,
            long maxRecordingDataPoints,
            int maxSatellites,
            bool allowTextToSpeech,
            IList<string> features)
        {
            var key = CoreContext.LicenseKeys.SingleOrDefault(a => a.Version == version.Major);

            if (key == null)
            {
                throw new ArgumentException("Could not find key to sign the license");
            }

            var license = Standard.Licensing.License.New()
                .WithUniqueIdentifier(licenseId)
                .As(type)
                .ExpiresAt(expires)
                .WithMaximumUtilization(1)
                .WithProductFeatures(new Dictionary<string, string>
                {
                    {"MaxDatapoints", maxDatapoints.ToString()},
                    {"MaxUsers", maxUsers.ToString()},
                    {"AllowRemoteControl", allowRemoteControl.ToString()},
                    {"MaxRemoteTunnels", maxRemoteTunnels.ToString()},
                    {"MaxRecordingDataPoints", $"{maxRecordingDataPoints}"},
                    {"MaxSatellites", $"{maxSatellites}"},
                    {"AllowTextToSpeech", allowTextToSpeech.ToString()}
                })
                .LicensedTo(licensedTo, email);

            license.WithProductFeatures((features ?? new List<string>()).Distinct().ToDictionary(a => a, a => "true"));

            var lic = license.CreateAndSignWithPrivateKey(key.PrivateKey, GetPassphrase(version.Major));

            return lic.ToString();
        }

        public string CreateDemoLicense()
        {
            return Create(LicenseType.Trial, 100, 1, new Version(0, 0), Guid.NewGuid(), new DateTime(2030, 12, 31), "Anybody", "nobody@nobody.nobody", false, 0, 10, 0, false, new List<string>());
        }
    }
}
