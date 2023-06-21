using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.Cloud.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddRemoteControlPortEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RemoteControlPorts",
                columns: table => new
                {
                    Port = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    This2CoreServer = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastUsed = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PortType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    This2DriverId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemoteControlPorts", x => x.Port);
                    table.ForeignKey(
                        name: "FK_RemoteControlPorts_CoreServers_This2CoreServer",
                        column: x => x.This2CoreServer,
                        principalTable: "CoreServers",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RemoteControlPorts_This2CoreServer",
                table: "RemoteControlPorts",
                column: "This2CoreServer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RemoteControlPorts");
        }
    }
}
