using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.Cloud.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddDockerVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsPreRelease",
                table: "Plugins",
                newName: "IsPrerelease");

            migrationBuilder.CreateTable(
                name: "DockerVersions",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageTag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangeLog = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPreRelease = table.Column<bool>(type: "bit", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    Branch = table.Column<string>(type: "nvarchar(max)", nullable: true, defaultValue: "develop")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DockerVersions", x => x.ObjId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DockerVersions");

            migrationBuilder.RenameColumn(
                name: "IsPrerelease",
                table: "Plugins",
                newName: "IsPreRelease");
        }
    }
}
