using Microsoft.EntityFrameworkCore.Migrations;

namespace Automatica.Core.Cloud.EF.Migrations
{
    public partial class AddComponentNameToPlugin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ComponentName",
                table: "Plugins",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComponentName",
                table: "Plugins");
        }
    }
}
