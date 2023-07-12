using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.Cloud.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxRemoteTunnelsToLicenseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxRemoteTunnels",
                table: "Licenses",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxRemoteTunnels",
                table: "Licenses");
        }
    }
}
