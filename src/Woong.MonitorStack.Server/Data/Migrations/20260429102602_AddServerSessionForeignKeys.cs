using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woong.MonitorStack.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddServerSessionForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_focus_sessions_DeviceId_ClientSessionId",
                table: "focus_sessions",
                columns: new[] { "DeviceId", "ClientSessionId" });

            migrationBuilder.CreateIndex(
                name: "IX_web_sessions_DeviceId_FocusSessionId",
                table: "web_sessions",
                columns: new[] { "DeviceId", "FocusSessionId" });

            migrationBuilder.AddForeignKey(
                name: "FK_device_state_sessions_devices_DeviceId",
                table: "device_state_sessions",
                column: "DeviceId",
                principalTable: "devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_focus_sessions_devices_DeviceId",
                table: "focus_sessions",
                column: "DeviceId",
                principalTable: "devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_raw_events_devices_DeviceId",
                table: "raw_events",
                column: "DeviceId",
                principalTable: "devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_web_sessions_devices_DeviceId",
                table: "web_sessions",
                column: "DeviceId",
                principalTable: "devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_web_sessions_focus_sessions_DeviceId_FocusSessionId",
                table: "web_sessions",
                columns: new[] { "DeviceId", "FocusSessionId" },
                principalTable: "focus_sessions",
                principalColumns: new[] { "DeviceId", "ClientSessionId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_device_state_sessions_devices_DeviceId",
                table: "device_state_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_focus_sessions_devices_DeviceId",
                table: "focus_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_raw_events_devices_DeviceId",
                table: "raw_events");

            migrationBuilder.DropForeignKey(
                name: "FK_web_sessions_devices_DeviceId",
                table: "web_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_web_sessions_focus_sessions_DeviceId_FocusSessionId",
                table: "web_sessions");

            migrationBuilder.DropIndex(
                name: "IX_web_sessions_DeviceId_FocusSessionId",
                table: "web_sessions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_focus_sessions_DeviceId_ClientSessionId",
                table: "focus_sessions");
        }
    }
}
