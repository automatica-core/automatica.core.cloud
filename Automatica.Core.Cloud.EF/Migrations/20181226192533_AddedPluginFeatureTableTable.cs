using Microsoft.EntityFrameworkCore.Migrations;

namespace Automatica.Core.Cloud.EF.Migrations
{
    public partial class AddedPluginFeatureTableTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PluginFeature_Plugins_This2Plugin",
                table: "PluginFeature");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PluginFeature",
                table: "PluginFeature");

            migrationBuilder.RenameTable(
                name: "PluginFeature",
                newName: "PluginFeatures");

            migrationBuilder.RenameIndex(
                name: "IX_PluginFeature_This2Plugin",
                table: "PluginFeatures",
                newName: "IX_PluginFeatures_This2Plugin");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PluginFeatures",
                table: "PluginFeatures",
                column: "ObjId");

            migrationBuilder.AddForeignKey(
                name: "FK_PluginFeatures_Plugins_This2Plugin",
                table: "PluginFeatures",
                column: "This2Plugin",
                principalTable: "Plugins",
                principalColumn: "ObjId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PluginFeatures_Plugins_This2Plugin",
                table: "PluginFeatures");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PluginFeatures",
                table: "PluginFeatures");

            migrationBuilder.RenameTable(
                name: "PluginFeatures",
                newName: "PluginFeature");

            migrationBuilder.RenameIndex(
                name: "IX_PluginFeatures_This2Plugin",
                table: "PluginFeature",
                newName: "IX_PluginFeature_This2Plugin");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PluginFeature",
                table: "PluginFeature",
                column: "ObjId");

            migrationBuilder.AddForeignKey(
                name: "FK_PluginFeature_Plugins_This2Plugin",
                table: "PluginFeature",
                column: "This2Plugin",
                principalTable: "Plugins",
                principalColumn: "ObjId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
