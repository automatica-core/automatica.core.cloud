using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.LicenseManager;
using Automatica.Core.Cloud.WebApi.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Automatica.Core.Cloud.WebApi.Controllers
{
    public class PublicPrivateKey
    {
        public string Public { get; set; }
        public string Version { get; set; }
    }

    public class GenerateLicenseData
    {
        public Guid ObjId { get; set; }
        public DateTime Expires { get; set; }

        public int MaxDatapoints { get; set; }
        public int MaxUsers { get; set; }

        public string LicensedTo { get; set; }
        public string Email { get; set; }

        public Guid This2CoreServer { get; set; }

        public bool AllowRemoteControl { get; set; }
        public IList<string> Features { get; set; }
    }

    [Route("webapi/v{version:apiVersion}/license"), ApiVersion("1.0"), AllowAnonymous]
    public class LicenseController : BaseController
    {
        public LicenseController(CoreContext context, ILicenseManager licenseManager)
        {
            Context = context;
            LicenseManager = licenseManager;
        }

        public CoreContext Context { get; }
        public ILicenseManager LicenseManager { get; }

        [HttpPost, Route("key/{coreServerVersion}/{apiKey}"), NeedsRole(UserRole.SystemAdministrator), UserApiKeyAuthorization]
        public PublicPrivateKey GeneratePublicPrivateKey(string coreServerVersion)
        {
            var vers = new Version(coreServerVersion);
            var publicKey = LicenseManager.GeneratePublicPrivateKey(vers);

            return new PublicPrivateKey
            {
                Public = publicKey,
                Version = vers.Major.ToString()
            };
        }

        [HttpGet, Route(""), AuthorizeRole(Role = UserRole.SystemAdministrator)]
        public IList<License> GetLicenses()
        {
            return Context.Licenses.Include(a => a.This2CoreServerNavigation).ToList();
        }

        [HttpGet, Route("demo"), AuthorizeRole(Role = UserRole.SystemAdministrator)]
        public string CreateDemoLicense()
        {
            return LicenseManager.CreateDemoLicense();
        }


        [HttpGet, Route("features"), AuthorizeRole(Role = UserRole.SystemAdministrator)]
        public List<PluginFeature> GetAllPluginFeatures()
        {
            var pluginFeatures = Context.PluginFeatures.Distinct().ToList();

           var distinctFeatures = pluginFeatures
                .GroupBy(p => p.FeatureName)
                .Select(g => g.First())
                .ToList();

           return distinctFeatures;
        }

        [HttpDelete, Route("{objId}"), AuthorizeRole(Role = UserRole.SystemAdministrator)]
        public void Delete(Guid objId)
        {
            var coreServerVersion = Context.Licenses.SingleOrDefault(a => a.ObjId == objId);

            if (coreServerVersion != null)
            {
                Context.Remove(coreServerVersion);
                Context.SaveChanges();
            }
        }

        [HttpPost, Route("license/{apiKey}"), NeedsRole(UserRole.SystemAdministrator), AuthorizeRole(Role = UserRole.SystemAdministrator)]
        public License GenerateLicense( [FromBody]GenerateLicenseData generateLicenseData)
        {
            var coreServer = Context.CoreServers.Single(a => a.ObjId == generateLicenseData.This2CoreServer);
            var key = Context.LicenseKeys.SingleOrDefault(a => a.Version == coreServer.VersionObj.Major);

            var dbLicense = Context.Licenses.SingleOrDefault(a => a.ObjId == generateLicenseData.ObjId);
            var isNew = false;
            if (dbLicense == null)
            {
                isNew = true;
                dbLicense = new License();
            }

            dbLicense.This2CoreServer = generateLicenseData.This2CoreServer;
            dbLicense.This2VersionKey = key.ObjId;
            dbLicense.LicenseKey = LicenseManager.CreateLicense(generateLicenseData.MaxDatapoints, generateLicenseData.MaxUsers, generateLicenseData.This2CoreServer, generateLicenseData.Expires, generateLicenseData.LicensedTo, generateLicenseData.Email, generateLicenseData.AllowRemoteControl, generateLicenseData.Features); 
            dbLicense.MaxDatapoints = generateLicenseData.MaxDatapoints;
            dbLicense.MaxUsers = generateLicenseData.MaxUsers;
            dbLicense.LicensedTo = generateLicenseData.LicensedTo;
            dbLicense.Email = generateLicenseData.LicensedTo;
            dbLicense.FeaturesString = string.Join(",", generateLicenseData.Features);
            dbLicense.AllowRemoteControl = generateLicenseData.AllowRemoteControl;

            if (isNew)
            {
                Context.Add(dbLicense);
            }
            else
            {
                Context.Update(dbLicense);
            }
            Context.SaveChanges();

            return dbLicense;
        }
    }
}
