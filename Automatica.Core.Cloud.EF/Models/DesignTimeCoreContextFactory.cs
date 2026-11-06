using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Automatica.Core.Cloud.EF.Models
{
    /// <summary>Only used by "dotnet ef migrations add"; never connects anywhere.</summary>
    internal class DesignTimeCoreContextFactory : IDesignTimeDbContextFactory<CoreContext>
    {
        public CoreContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<CoreContext>()
                .UseSqlServer("Server=localhost;Database=design-time-only;Integrated Security=true")
                .Options;
            return new CoreContext(options);
        }
    }
}
