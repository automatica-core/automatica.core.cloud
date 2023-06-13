using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.Cloud.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddNewCoreFeatureInLicenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowRemoteControl",
                table: "Licenses",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowRemoteControl",
                table: "Licenses");
        }
    }
}
