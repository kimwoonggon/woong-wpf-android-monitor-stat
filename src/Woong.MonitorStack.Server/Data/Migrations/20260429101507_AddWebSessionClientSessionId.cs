using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woong.MonitorStack.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWebSessionClientSessionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_web_sessions_DeviceId_FocusSessionId_StartedAtUtc_EndedAtUt~",
                table: "web_sessions");

            migrationBuilder.AddColumn<string>(
                name: "ClientSessionId",
                table: "web_sessions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE web_sessions
                SET "ClientSessionId" = 'legacy-web-session-' || "Id"::text
                WHERE "ClientSessionId" IS NULL OR "ClientSessionId" = ''
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ClientSessionId",
                table: "web_sessions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_web_sessions_DeviceId_ClientSessionId",
                table: "web_sessions",
                columns: new[] { "DeviceId", "ClientSessionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_web_sessions_DeviceId_ClientSessionId",
                table: "web_sessions");

            migrationBuilder.DropColumn(
                name: "ClientSessionId",
                table: "web_sessions");

            migrationBuilder.CreateIndex(
                name: "IX_web_sessions_DeviceId_FocusSessionId_StartedAtUtc_EndedAtUt~",
                table: "web_sessions",
                columns: new[] { "DeviceId", "FocusSessionId", "StartedAtUtc", "EndedAtUtc", "Url" },
                unique: true);
        }
    }
}
