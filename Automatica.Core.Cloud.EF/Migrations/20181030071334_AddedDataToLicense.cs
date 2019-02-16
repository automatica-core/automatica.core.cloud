using Microsoft.EntityFrameworkCore.Migrations;

namespace Automatica.Core.Cloud.EF.Migrations
{
    public partial class AddedDataToLicense : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Licenses",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicensedTo",
                table: "Licenses",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Licenses");

            migrationBuilder.DropColumn(
                name: "LicensedTo",
                table: "Licenses");
        }
    }
}
