using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.Cloud.EF.Migrations
{
    /// <inheritdoc />
    public partial class ExtendLicenseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Licenses",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2019, 1, 31, 23, 59, 59, 59, DateTimeKind.Local));

            migrationBuilder.AddColumn<int>(
                name: "MaxSatellites",
                table: "Licenses",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Licenses");

            migrationBuilder.DropColumn(
                name: "MaxSatellites",
                table: "Licenses");
        }
    }
}
