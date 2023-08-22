using System;
using System.Collections.Generic;

namespace Automatica.Core.Cloud.LicenseManager
{
    public interface ILicenseManager
    {
        string CreateLicense(int maxDatapoints, int maxUsers, Guid this2CoreServer, DateTime expires, string licensedTo, string email, bool allowRemoteControl, int maxRemoteTunnels, long maxRecordingDataPoints, IList<string> features);
        string CreateTrialLicense(int maxDatapoints, int maxUsers, Guid this2CoreServer, DateTime expires, string licensedTo, string email);

        string CreateDemoLicense();
        string GeneratePublicPrivateKey(Version version);
    }
}
