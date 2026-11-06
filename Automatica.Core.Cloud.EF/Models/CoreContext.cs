using Microsoft.EntityFrameworkCore;

namespace Automatica.Core.Cloud.EF.Models
{
    public class CoreContext : DbContext
    {
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<ServerVersion> Versions { get; set; }
        public virtual DbSet<ServerDockerVersion> DockerVersions { get; set; }
        public virtual DbSet<Plugin> Plugins { get; set; }
        public virtual DbSet<PluginFeature> PluginFeatures { get; set; }
        public virtual DbSet<CoreServer> CoreServers { get; set; }
        public virtual DbSet<LicenseKey> LicenseKeys { get; set; }

        public virtual DbSet<License> Licenses { get; set; }
        public virtual DbSet<RemoteControlPort> RemoteControlPorts { get; set; }
        public virtual DbSet<RemoteControlSubDomain> RemoteControlSubDomains { get; set; }

        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
        public virtual DbSet<DeployKey> DeployKeys { get; set; }
        public virtual DbSet<MailRecipient> MailRecipients { get; set; }

        public CoreContext(DbContextOptions<CoreContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ServerVersion>(entity =>
            {
                entity.HasKey(a => a.ObjId);
                entity.Property(a => a.ObjId).ValueGeneratedOnAdd();
                entity.Property(a => a.Branch).HasDefaultValue("develop");
            });

            modelBuilder.Entity<ServerDockerVersion>(entity =>
            {
                entity.HasKey(a => a.ObjId);
                entity.Property(a => a.ObjId).ValueGeneratedOnAdd();
                entity.Property(a => a.Branch).HasDefaultValue("develop");
            });

            modelBuilder.Entity<Plugin>(entity =>
            {
                entity.HasKey(a => a.ObjId);
                entity.HasOne(a => a.This2UserNavigation).WithMany().HasForeignKey(a => a.This2User);

                entity.Property(a => a.ObjId).ValueGeneratedOnAdd();
                entity.Property(a => a.Branch).HasDefaultValue("develop");
            });

            modelBuilder.Entity<PluginFeature>(entity =>
            {
                entity.HasKey(a => a.ObjId);
                entity.HasOne(a => a.This2PluginNavigation).WithMany(a => a.LicenseFeatures).HasForeignKey(a => a.This2Plugin);
                entity.Property(a => a.FeatureName).IsRequired();

                entity.Property(a => a.ObjId).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<CoreServer>(entity =>
            {
                entity.HasKey(a => a.ObjId);
                entity.Property(a => a.ObjId).ValueGeneratedOnAdd();
                entity.Property(a => a.Version).HasDefaultValue("0.0.0.0");
                entity.Property(a => a.ApiKeyHash).HasMaxLength(32);
                entity.HasIndex(a => a.ApiKeyHash);
                entity.HasIndex(a => a.ServerGuid);

                entity.HasOne(a => a.This2UserNavigation).WithMany().HasForeignKey(a => a.This2User);
            });

            modelBuilder.Entity<LicenseKey>(entity =>
            {
                entity.HasKey(a => a.ObjId);
                entity.Property(a => a.ObjId).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<License>(entity =>
            {
                entity.HasKey(a => a.ObjId);
                entity.Property(a => a.ObjId).ValueGeneratedOnAdd();
                entity.HasOne(a => a.This2CoreServerNavigation).WithMany().HasForeignKey(a => a.This2CoreServer);
                entity.HasOne(a => a.This2VersionKeyNavigation).WithMany().HasForeignKey(a => a.This2VersionKey);
            });

            modelBuilder.Entity<RemoteControlPort>(entity =>
            {
                entity.HasKey(a => a.Port);
                entity.Property(a => a.Port).ValueGeneratedOnAdd();
                entity.HasOne(a => a.This2CoreServerNavigation).WithMany().HasForeignKey(a => a.This2CoreServer);
            });
            modelBuilder.Entity<RemoteControlSubDomain>(entity =>
            {
                entity.HasKey(a => a.ObjId);
                entity.Property(a => a.ObjId).ValueGeneratedOnAdd();
                entity.HasOne(a => a.This2CoreServerNavigation).WithMany().HasForeignKey(a => a.This2CoreServer);
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(a => a.ObjId);
                entity.Property(a => a.ObjId).ValueGeneratedOnAdd();
                entity.Property(a => a.TokenHash).HasMaxLength(32).IsRequired();
                entity.HasIndex(a => a.TokenHash).IsUnique();
                entity.HasIndex(a => a.FamilyId);
                entity.Property(a => a.CreatedByIp).HasMaxLength(64);
                entity.Property(a => a.SecurityStamp).HasMaxLength(64);
                entity.HasOne(a => a.This2UserNavigation).WithMany().HasForeignKey(a => a.This2User).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DeployKey>(entity =>
            {
                entity.HasKey(a => a.ObjId);
                entity.Property(a => a.ObjId).ValueGeneratedOnAdd();
                entity.Property(a => a.Name).HasMaxLength(100).IsRequired();
                entity.Property(a => a.KeyHash).HasMaxLength(32).IsRequired();
                entity.HasIndex(a => a.KeyHash).IsUnique();
                entity.Property(a => a.KeyPrefix).HasMaxLength(16);
                entity.Property(a => a.AllowedBranches).HasMaxLength(200);
                entity.Property(a => a.LastUsedIp).HasMaxLength(64);
                entity.HasOne(a => a.This2UserNavigation).WithMany().HasForeignKey(a => a.This2User).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MailRecipient>(entity =>
            {
                entity.HasKey(a => a.ObjId);
                entity.Property(a => a.ObjId).ValueGeneratedOnAdd();
                entity.Property(a => a.Email).HasMaxLength(254).IsRequired();
                entity.Property(a => a.VerificationTokenHash).HasMaxLength(32);
                entity.HasIndex(a => new { a.This2CoreServer, a.Email }).IsUnique();
                entity.HasOne(a => a.This2CoreServerNavigation).WithMany().HasForeignKey(a => a.This2CoreServer).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(a => a.SecurityStamp).HasMaxLength(64);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
