using Automatica.Core.Cloud.EF.Models;
using Standard.Licensing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Automatica.Core.Cloud.LicenseManager
{
    public class LicenseManager : ILicenseManager
    {
        private const string MySecretLicenseBla = "`SzCQ975c.yN~9@N";

        public LicenseManager(CoreContext coreContext)
        {
            CoreContext = coreContext;
        }

        public CoreContext CoreContext { get; }

        public string GeneratePublicPrivateKey(System.Version version)
        {
            var vers = CoreContext.LicenseKeys.SingleOrDefault(a => a.Version == version.Major);

            var privateKey = "";
            var publicKey = "";
            if (vers == null)
            {
                var keyGenerator = Standard.Licensing.Security.Cryptography.KeyGenerator.Create();
                var keyPair = keyGenerator.GenerateKeyPair();
                privateKey = keyPair.ToEncryptedPrivateKeyString(version.Major + version.Major + version.Major + version.Major + MySecretLicenseBla);
                publicKey = keyPair.ToPublicKeyString();

                vers = new LicenseKey
                {
                    PrivateKey = privateKey,
                    PublicKey = publicKey,
                    Version = version.Major,
                    ObjId = Guid.NewGuid()
                };

                CoreContext.Add(vers);
                CoreContext.SaveChanges();
            }
            else
            {
                privateKey = vers.PrivateKey;
            }
            return publicKey;

        }

        public string CreateLicense(int maxDatapoints, int maxUsers, Guid this2CoreServer, DateTime expires, string licensedTo, string email, bool allowRemoteControl, IList<string> features)
        {
            return Create(LicenseType.Standard, maxDatapoints, maxUsers, this2CoreServer, expires, licensedTo, email, allowRemoteControl, features);
        }

        public string CreateTrialLicense(int maxDatapoints, int maxUsers, Guid this2CoreServer, DateTime expires, string licensedTo, string email)
        {
            return Create(LicenseType.Trial, maxDatapoints, maxUsers, this2CoreServer, expires, licensedTo, email, false, new List<string>());
        }

        private string Create(LicenseType type, int maxDatapoints, int maxUsers, Guid this2CoreServer, DateTime expires, string licensedTo, string email, bool allowRemoteControl, IList<string> features)
        {
            var coreServer = CoreContext.CoreServers.Single(a => a.ObjId == this2CoreServer);
            return Create(type, maxDatapoints, maxUsers, coreServer.VersionObj, coreServer.ServerGuid, expires, licensedTo, email, allowRemoteControl, features);
        }

        private string Create(LicenseType type, int maxDatapoints, int maxUsers, System.Version version, Guid licenseId, DateTime expires, string licensedTo, string email, bool allowRemoteControl, IList<string> features)
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
                    {"AllowRemoteControl", allowRemoteControl.ToString()}
                })
                .LicensedTo(licensedTo, email);

            license.WithProductFeatures(features.ToDictionary(a => a, a => "true"));

            var lic = license.CreateAndSignWithPrivateKey(key.PrivateKey, version.Major + version.Major + version.Major + version.Major + MySecretLicenseBla);

            return lic.ToString();
        }

        public string CreateDemoLicense()
        {
            return Create(LicenseType.Trial, 100, 1, new System.Version(0, 0), Guid.NewGuid(),new DateTime(2030, 12, 31), "Anybody", "nobody@nobody.nobody", false, new List<string>());
        }
    }
}
