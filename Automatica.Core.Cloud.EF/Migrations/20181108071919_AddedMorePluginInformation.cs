using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Automatica.Core.Cloud.EF.Migrations
{
    public partial class AddedMorePluginInformation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Publisher",
                table: "Plugins",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "This2User",
                table: "Plugins",
                nullable: false,
                defaultValue: new Guid("163b2739-1f9f-41e7-9e5e-acbfc50da999"));

            migrationBuilder.CreateIndex(
                name: "IX_Plugins_This2User",
                table: "Plugins",
                column: "This2User");

            migrationBuilder.AddForeignKey(
                name: "FK_Plugins_Users_This2User",
                table: "Plugins",
                column: "This2User",
                principalTable: "Users",
                principalColumn: "ObjId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Plugins_Users_This2User",
                table: "Plugins");

            migrationBuilder.DropIndex(
                name: "IX_Plugins_This2User",
                table: "Plugins");

            migrationBuilder.DropColumn(
                name: "Publisher",
                table: "Plugins");

            migrationBuilder.DropColumn(
                name: "This2User",
                table: "Plugins");
        }
    }
}
