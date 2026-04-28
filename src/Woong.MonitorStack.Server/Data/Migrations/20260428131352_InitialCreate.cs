using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Woong.MonitorStack.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_summaries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SummaryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TimezoneId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TotalActiveMs = table.Column<long>(type: "bigint", nullable: false),
                    TotalIdleMs = table.Column<long>(type: "bigint", nullable: false),
                    TotalWebMs = table.Column<long>(type: "bigint", nullable: false),
                    TopAppsJson = table.Column<string>(type: "text", nullable: false),
                    TopDomainsJson = table.Column<string>(type: "text", nullable: false),
                    GeneratedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_summaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Platform = table.Column<int>(type: "integer", nullable: false),
                    DeviceKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TimezoneId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "focus_sessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientSessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PlatformAppKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TimezoneId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsIdle = table.Column<bool>(type: "boolean", nullable: false),
                    Source = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_focus_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "raw_events",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientEventId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_raw_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "web_sessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    FocusSessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    BrowserFamily = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Url = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: false),
                    Domain = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    PageTitle = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_web_sessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_summaries_UserId_SummaryDate_TimezoneId",
                table: "daily_summaries",
                columns: new[] { "UserId", "SummaryDate", "TimezoneId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_devices_UserId_Platform_DeviceKey",
                table: "devices",
                columns: new[] { "UserId", "Platform", "DeviceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_focus_sessions_DeviceId_ClientSessionId",
                table: "focus_sessions",
                columns: new[] { "DeviceId", "ClientSessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_raw_events_DeviceId_ClientEventId",
                table: "raw_events",
                columns: new[] { "DeviceId", "ClientEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_web_sessions_DeviceId_FocusSessionId_StartedAtUtc_EndedAtUt~",
                table: "web_sessions",
                columns: new[] { "DeviceId", "FocusSessionId", "StartedAtUtc", "EndedAtUtc", "Url" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_summaries");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "focus_sessions");

            migrationBuilder.DropTable(
                name: "raw_events");

            migrationBuilder.DropTable(
                name: "web_sessions");
        }
    }
}
