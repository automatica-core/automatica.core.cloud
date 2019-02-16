using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Automatica.Core.Cloud.EF.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LicenseKeys",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    PublicKey = table.Column<string>(nullable: true),
                    PrivateKey = table.Column<string>(nullable: true),
                    Version = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenseKeys", x => x.ObjId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    UserName = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    PasswordHash = table.Column<string>(nullable: true),
                    Salt = table.Column<string>(nullable: true),
                    FirstName = table.Column<string>(nullable: true),
                    LastName = table.Column<string>(nullable: true),
                    Enabled = table.Column<bool>(nullable: false),
                    ActivationCode = table.Column<string>(nullable: true),
                    UserRole = table.Column<int>(nullable: false),
                    ApiKey = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.ObjId);
                });

            migrationBuilder.CreateTable(
                name: "CoreServers",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    ServerGuid = table.Column<Guid>(nullable: true),
                    ServerName = table.Column<string>(nullable: true),
                    LastKnownConnection = table.Column<DateTime>(nullable: true),
                    Version = table.Column<string>(nullable: true, defaultValue: "0.0.0.0"),
                    Rid = table.Column<string>(nullable: true),
                    ApiKey = table.Column<Guid>(nullable: false),
                    This2User = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoreServers", x => x.ObjId);
                    table.ForeignKey(
                        name: "FK_CoreServers_Users_This2User",
                        column: x => x.This2User,
                        principalTable: "Users",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoreServerVersions",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Version = table.Column<string>(nullable: true),
                    AzureUrl = table.Column<string>(nullable: true),
                    AzureFileName = table.Column<string>(nullable: true),
                    ChangeLog = table.Column<string>(nullable: true),
                    This2User = table.Column<Guid>(nullable: false),
                    IsPrerelease = table.Column<bool>(nullable: false),
                    IsPublic = table.Column<bool>(nullable: false),
                    Rid = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoreServerVersions", x => x.ObjId);
                    table.ForeignKey(
                        name: "FK_CoreServerVersions_Users_This2User",
                        column: x => x.This2User,
                        principalTable: "Users",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Licenses",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    This2CoreServer = table.Column<Guid>(nullable: false),
                    LicenseKey = table.Column<string>(nullable: true),
                    This2VersionKey = table.Column<Guid>(nullable: false),
                    MaxDatapoints = table.Column<int>(nullable: false),
                    MaxUsers = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Licenses", x => x.ObjId);
                    table.ForeignKey(
                        name: "FK_Licenses_CoreServers_This2CoreServer",
                        column: x => x.This2CoreServer,
                        principalTable: "CoreServers",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Licenses_LicenseKeys_This2VersionKey",
                        column: x => x.This2VersionKey,
                        principalTable: "LicenseKeys",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoreServers_This2User",
                table: "CoreServers",
                column: "This2User");

            migrationBuilder.CreateIndex(
                name: "IX_CoreServerVersions_This2User",
                table: "CoreServerVersions",
                column: "This2User");

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_This2CoreServer",
                table: "Licenses",
                column: "This2CoreServer");

            migrationBuilder.CreateIndex(
                name: "IX_Licenses_This2VersionKey",
                table: "Licenses",
                column: "This2VersionKey");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoreServerVersions");

            migrationBuilder.DropTable(
                name: "Licenses");

            migrationBuilder.DropTable(
                name: "CoreServers");

            migrationBuilder.DropTable(
                name: "LicenseKeys");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
