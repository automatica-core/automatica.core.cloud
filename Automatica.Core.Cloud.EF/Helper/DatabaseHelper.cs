using Automatica.Core.Cloud.EF.Models;
using Automatica.Core.Cloud.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Automatica.Core.Cloud.EF.Helper
{
    public static class DatabaseHelper
    {
        public static IServiceProvider Migrate(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.GetService<IServiceScopeFactory>().CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<CoreContext>();
            dbContext.Database.Migrate();
            bool dbCreated = !dbContext.Users.Any();
            if(dbCreated)
            {
                OnAdd(dbContext);
            }

            return serviceProvider;
        }

        private static void OnAdd(CoreContext context)
        {
            var salt = User.GenerateNewSalt();
            var saUser = new User
            {
                UserName = "sa",
                FirstName = "admin",
                LastName = "admin",
                Salt = salt,
                PasswordHash = User.HashPassword("thisisaverysecurepassword123$", salt),
                ObjId = new Guid("163b2739-1f9f-41e7-9e5e-acbfc50da999"),
                UserRole = UserRole.SystemAdministrator,
                ApiKey = new Guid("9b4771f9-411f-4d63-be4f-ae592a9eb251"),
                PublisherName = "automatica.core"
            };
            
            var licenseKey = new LicenseKey
            {
                ObjId = new Guid("91de628c-2c22-410f-8b6b-9a7aab74f2e5"),
                PrivateKey = "MHcwIwYKKoZIhvcNAQwBAzAVBBBC8/Tl8YVoDZNCmY+uw+L5AgEKBFDE3+R8PvWHwohMm43WXuacCWoNB3eDZAIDvaxJSq6OpyG3MqT6Y8ED5yavlBIIz0ttfKc8YkQDENumJS4SS9xWCg/EXRRYP/jQXpMirgGKVA==",
                PublicKey = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAENvdaBihxX8vS9n3XGi1f1Jb0u5cd+RE9p47z6Dspl69vmbCF4dMhUagGP4VzCoAlU4COdk5i7kyxK2KP+DVICw==",
                Version = 0
            };

            context.Add(licenseKey);

           
            context.Add(saUser);
            context.SaveChanges();
        }
    }
}
