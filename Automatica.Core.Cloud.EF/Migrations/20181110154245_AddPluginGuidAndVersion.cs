using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Automatica.Core.Cloud.EF.Migrations
{
    public partial class AddPluginGuidAndVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PluginGuid",
                table: "Plugins",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "PluginVersion",
                table: "Plugins",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PluginGuid",
                table: "Plugins");

            migrationBuilder.DropColumn(
                name: "PluginVersion",
                table: "Plugins");
        }
    }
}
