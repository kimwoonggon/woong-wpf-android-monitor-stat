using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woong.MonitorStack.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFocusSessionWindowMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "web_sessions",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(4096)",
                oldMaxLength: 4096);

            migrationBuilder.AlterColumn<string>(
                name: "PageTitle",
                table: "web_sessions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AddColumn<string>(
                name: "CaptureConfidence",
                table: "web_sessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CaptureMethod",
                table: "web_sessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivateOrUnknown",
                table: "web_sessions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessId",
                table: "focus_sessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessName",
                table: "focus_sessions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessPath",
                table: "focus_sessions",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "WindowHandle",
                table: "focus_sessions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WindowTitle",
                table: "focus_sessions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaptureConfidence",
                table: "web_sessions");

            migrationBuilder.DropColumn(
                name: "CaptureMethod",
                table: "web_sessions");

            migrationBuilder.DropColumn(
                name: "IsPrivateOrUnknown",
                table: "web_sessions");

            migrationBuilder.DropColumn(
                name: "ProcessId",
                table: "focus_sessions");

            migrationBuilder.DropColumn(
                name: "ProcessName",
                table: "focus_sessions");

            migrationBuilder.DropColumn(
                name: "ProcessPath",
                table: "focus_sessions");

            migrationBuilder.DropColumn(
                name: "WindowHandle",
                table: "focus_sessions");

            migrationBuilder.DropColumn(
                name: "WindowTitle",
                table: "focus_sessions");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "web_sessions",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(4096)",
                oldMaxLength: 4096,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PageTitle",
                table: "web_sessions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);
        }
    }
}
