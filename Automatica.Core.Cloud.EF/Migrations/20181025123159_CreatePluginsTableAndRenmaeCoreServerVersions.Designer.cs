﻿// <auto-generated />
using System;
using Automatica.Core.Cloud.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Automatica.Core.Cloud.EF.Migrations
{
    [DbContext(typeof(CoreContext))]
    [Migration("20181025123159_CreatePluginsTableAndRenmaeCoreServerVersions")]
    partial class CreatePluginsTableAndRenmaeCoreServerVersions
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.0-preview3-35497")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Automatica.Core.Cloud.EF.Models.CoreServer", b =>
                {
                    b.Property<Guid>("ObjId")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("ApiKey")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime?>("LastKnownConnection");

                    b.Property<string>("Rid");

                    b.Property<Guid?>("ServerGuid");

                    b.Property<string>("ServerName");

                    b.Property<Guid>("This2User");

                    b.Property<string>("Version")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValue("0.0.0.0");

                    b.HasKey("ObjId");

                    b.HasIndex("This2User");

                    b.ToTable("CoreServers");
                });

            modelBuilder.Entity("Automatica.Core.Cloud.EF.Models.License", b =>
                {
                    b.Property<Guid>("ObjId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("LicenseKey");

                    b.Property<int>("MaxDatapoints");

                    b.Property<int>("MaxUsers");

                    b.Property<Guid>("This2CoreServer");

                    b.Property<Guid>("This2VersionKey");

                    b.HasKey("ObjId");

                    b.HasIndex("This2CoreServer");

                    b.HasIndex("This2VersionKey");

                    b.ToTable("Licenses");
                });

            modelBuilder.Entity("Automatica.Core.Cloud.EF.Models.LicenseKey", b =>
                {
                    b.Property<Guid>("ObjId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("PrivateKey");

                    b.Property<string>("PublicKey");

                    b.Property<int>("Version");

                    b.HasKey("ObjId");

                    b.ToTable("LicenseKeys");
                });

            modelBuilder.Entity("Automatica.Core.Cloud.EF.Models.Plugin", b =>
                {
                    b.Property<Guid>("ObjId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AzureFileName");

                    b.Property<string>("AzureUrl");

                    b.Property<bool>("IsPrerelease");

                    b.Property<bool>("IsPublic");

                    b.Property<string>("MinCoreServerVersion");

                    b.Property<string>("Name");

                    b.Property<int>("PluginType");

                    b.Property<string>("Version");

                    b.HasKey("ObjId");

                    b.ToTable("Plugins");
                });

            modelBuilder.Entity("Automatica.Core.Cloud.EF.Models.ServerVersion", b =>
                {
                    b.Property<Guid>("ObjId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AzureFileName");

                    b.Property<string>("AzureUrl");

                    b.Property<string>("ChangeLog");

                    b.Property<bool>("IsPrerelease");

                    b.Property<bool>("IsPublic");

                    b.Property<string>("Rid");

                    b.Property<string>("Version");

                    b.HasKey("ObjId");

                    b.ToTable("Versions");
                });

            modelBuilder.Entity("Automatica.Core.Cloud.EF.Models.User", b =>
                {
                    b.Property<Guid>("ObjId")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ActivationCode");

                    b.Property<Guid>("ApiKey");

                    b.Property<string>("Email");

                    b.Property<bool>("Enabled");

                    b.Property<string>("FirstName");

                    b.Property<string>("LastName");

                    b.Property<string>("PasswordHash");

                    b.Property<string>("Salt");

                    b.Property<string>("UserName");

                    b.Property<int>("UserRole");

                    b.HasKey("ObjId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Automatica.Core.Cloud.EF.Models.CoreServer", b =>
                {
                    b.HasOne("Automatica.Core.Cloud.EF.Models.User", "This2UserNavigation")
                        .WithMany()
                        .HasForeignKey("This2User")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Automatica.Core.Cloud.EF.Models.License", b =>
                {
                    b.HasOne("Automatica.Core.Cloud.EF.Models.CoreServer", "This2CoreServerNavigation")
                        .WithMany()
                        .HasForeignKey("This2CoreServer")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Automatica.Core.Cloud.EF.Models.LicenseKey", "This2VersionKeyNavigation")
                        .WithMany()
                        .HasForeignKey("This2VersionKey")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
