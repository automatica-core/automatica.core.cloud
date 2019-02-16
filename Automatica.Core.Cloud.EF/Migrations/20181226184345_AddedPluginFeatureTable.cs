using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Automatica.Core.Cloud.EF.Migrations
{
    public partial class AddedPluginFeatureTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PluginFeature",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(nullable: false),
                    This2Plugin = table.Column<Guid>(nullable: false),
                    FeatureName = table.Column<string>(nullable: false),
                    IsMandatory = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PluginFeature", x => x.ObjId);
                    table.ForeignKey(
                        name: "FK_PluginFeature_Plugins_This2Plugin",
                        column: x => x.This2Plugin,
                        principalTable: "Plugins",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PluginFeature_This2Plugin",
                table: "PluginFeature",
                column: "This2Plugin");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PluginFeature");
        }
    }
}
