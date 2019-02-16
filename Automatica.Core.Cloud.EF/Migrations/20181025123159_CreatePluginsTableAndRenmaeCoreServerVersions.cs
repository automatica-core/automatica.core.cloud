using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Automatica.Core.Cloud.EF.Migrations
{
    public partial class CreatePluginsTableAndRenmaeCoreServerVersions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoreServerVersions");

            migrationBuilder.CreateTable(
                name: "Plugins",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Version = table.Column<string>(nullable: true),
                    PluginType = table.Column<int>(nullable: false),
                    MinCoreServerVersion = table.Column<string>(nullable: true),
                    AzureUrl = table.Column<string>(nullable: true),
                    AzureFileName = table.Column<string>(nullable: true),
                    IsPublic = table.Column<bool>(nullable: false),
                    IsPrerelease = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plugins", x => x.ObjId);
                });

            migrationBuilder.CreateTable(
                name: "Versions",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    Version = table.Column<string>(nullable: true),
                    AzureUrl = table.Column<string>(nullable: true),
                    AzureFileName = table.Column<string>(nullable: true),
                    ChangeLog = table.Column<string>(nullable: true),
                    IsPrerelease = table.Column<bool>(nullable: false),
                    IsPublic = table.Column<bool>(nullable: false),
                    Rid = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Versions", x => x.ObjId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Plugins");

            migrationBuilder.DropTable(
                name: "Versions");

            migrationBuilder.CreateTable(
                name: "CoreServerVersions",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    AzureFileName = table.Column<string>(nullable: true),
                    AzureUrl = table.Column<string>(nullable: true),
                    ChangeLog = table.Column<string>(nullable: true),
                    IsPrerelease = table.Column<bool>(nullable: false),
                    IsPublic = table.Column<bool>(nullable: false),
                    Rid = table.Column<string>(nullable: true),
                    This2User = table.Column<Guid>(nullable: false),
                    Version = table.Column<string>(nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_CoreServerVersions_This2User",
                table: "CoreServerVersions",
                column: "This2User");
        }
    }
}
