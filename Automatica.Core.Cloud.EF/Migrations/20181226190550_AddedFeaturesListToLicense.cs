using Microsoft.EntityFrameworkCore.Migrations;

namespace Automatica.Core.Cloud.EF.Migrations
{
    public partial class AddedFeaturesListToLicense : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FeaturesString",
                table: "Licenses",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeaturesString",
                table: "Licenses");
        }
    }
}
