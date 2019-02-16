using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;

namespace Automatica.Core.Cloud.EF.Models
{
    public class CoreContext : DbContext
    {
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<ServerVersion> Versions { get; set; }
        public virtual DbSet<Plugin> Plugins { get; set; }
        public virtual DbSet<PluginFeature> PluginFeatures { get; set; }
        public virtual DbSet<CoreServer> CoreServers { get; set; }
        public virtual DbSet<LicenseKey> LicenseKeys { get; set; }

        public virtual DbSet<License> Licenses { get; set; }
        public IConfiguration Config { get; }

        public CoreContext(IConfiguration config)
        {
            Config = config;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(Config.GetConnectionString("AutomaticaCoreCloudSql"), (options) =>
                {
                    options.EnableRetryOnFailure();
                });
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ServerVersion>(entity =>
            {
                entity.HasKey(a => a.ObjId);
                entity.Property(a => a.ObjId).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<Plugin>(entity =>
            {
                entity.HasKey(a => a.ObjId);
                entity.HasOne(a => a.This2UserNavigation).WithMany().HasForeignKey(a => a.This2User);

                entity.Property(a => a.ObjId).ValueGeneratedOnAdd();
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
                entity.Property(a => a.ApiKey).ValueGeneratedOnAdd();
                entity.Property(a => a.Version).HasDefaultValue("0.0.0.0");

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

            base.OnModelCreating(modelBuilder);
        }

    }
}
