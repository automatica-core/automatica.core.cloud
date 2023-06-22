using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.Cloud.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddRemoteControlSubDomainEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RemoteControlSubDomains",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubDomain = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    This2CoreServer = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastUsed = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemoteControlSubDomains", x => x.ObjId);
                    table.ForeignKey(
                        name: "FK_RemoteControlSubDomains_CoreServers_This2CoreServer",
                        column: x => x.This2CoreServer,
                        principalTable: "CoreServers",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RemoteControlSubDomains_This2CoreServer",
                table: "RemoteControlSubDomains",
                column: "This2CoreServer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RemoteControlSubDomains");
        }
    }
}
