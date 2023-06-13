using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.Cloud.EF.Migrations
{
    /// <inheritdoc />
    public partial class RenameNgrokFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastKnownNgrokUrlDate",
                table: "CoreServers",
                newName: "LastKnownRemoteConnectUrlDate");

            migrationBuilder.RenameColumn(
                name: "LastKnownNgrokUrl",
                table: "CoreServers",
                newName: "LastKnownRemoteConnectUrl");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastKnownRemoteConnectUrlDate",
                table: "CoreServers",
                newName: "LastKnownNgrokUrlDate");

            migrationBuilder.RenameColumn(
                name: "LastKnownRemoteConnectUrl",
                table: "CoreServers",
                newName: "LastKnownNgrokUrl");
        }
    }
}
