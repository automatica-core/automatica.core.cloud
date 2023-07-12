using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.Cloud.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddPluginGuidToRemoteControlSubDomainEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PluginGuid",
                table: "RemoteControlSubDomains",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PluginGuid",
                table: "RemoteControlSubDomains");
        }
    }
}
