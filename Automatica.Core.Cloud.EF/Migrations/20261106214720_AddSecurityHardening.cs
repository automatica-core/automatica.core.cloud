using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Automatica.Core.Cloud.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginCount",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApiKeyCreatedAt",
                table: "CoreServers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ApiKeyHash",
                table: "CoreServers",
                type: "varbinary(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeployKeys",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KeyHash = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: false),
                    KeyPrefix = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Scopes = table.Column<int>(type: "int", nullable: false),
                    AllowedBranches = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    This2User = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsedIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeployKeys", x => x.ObjId);
                    table.ForeignKey(
                        name: "FK_DeployKeys_Users_This2User",
                        column: x => x.This2User,
                        principalTable: "Users",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MailRecipients",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    This2CoreServer = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    VerificationTokenHash = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: true),
                    VerificationExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailRecipients", x => x.ObjId);
                    table.ForeignKey(
                        name: "FK_MailRecipients_CoreServers_This2CoreServer",
                        column: x => x.This2CoreServer,
                        principalTable: "CoreServers",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    ObjId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    This2User = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<byte[]>(type: "varbinary(32)", maxLength: 32, nullable: false),
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecurityStamp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByIp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.ObjId);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_This2User",
                        column: x => x.This2User,
                        principalTable: "Users",
                        principalColumn: "ObjId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoreServers_ApiKeyHash",
                table: "CoreServers",
                column: "ApiKeyHash");

            migrationBuilder.CreateIndex(
                name: "IX_CoreServers_ServerGuid",
                table: "CoreServers",
                column: "ServerGuid");

            migrationBuilder.CreateIndex(
                name: "IX_DeployKeys_KeyHash",
                table: "DeployKeys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeployKeys_This2User",
                table: "DeployKeys",
                column: "This2User");

            migrationBuilder.CreateIndex(
                name: "IX_MailRecipients_This2CoreServer_Email",
                table: "MailRecipients",
                columns: new[] { "This2CoreServer", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_FamilyId",
                table: "RefreshTokens",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_This2User",
                table: "RefreshTokens",
                column: "This2User");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            // The old app never checked Users.Enabled, so existing accounts may be stored as disabled.
            // Login now enforces it; keep every existing account working as before.
            migrationBuilder.Sql("UPDATE Users SET Enabled = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeployKeys");

            migrationBuilder.DropTable(
                name: "MailRecipients");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_CoreServers_ApiKeyHash",
                table: "CoreServers");

            migrationBuilder.DropIndex(
                name: "IX_CoreServers_ServerGuid",
                table: "CoreServers");

            migrationBuilder.DropColumn(
                name: "FailedLoginCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApiKeyCreatedAt",
                table: "CoreServers");

            migrationBuilder.DropColumn(
                name: "ApiKeyHash",
                table: "CoreServers");
        }
    }
}
