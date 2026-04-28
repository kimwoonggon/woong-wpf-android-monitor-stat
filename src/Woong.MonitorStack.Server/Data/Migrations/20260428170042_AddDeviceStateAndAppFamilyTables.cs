using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Woong.MonitorStack.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceStateAndAppFamilyTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_families",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_families", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "device_state_sessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientSessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StateType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TimezoneId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_state_sessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "app_family_mappings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppFamilyId = table.Column<long>(type: "bigint", nullable: false),
                    MappingType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MatchKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_family_mappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_app_family_mappings_app_families_AppFamilyId",
                        column: x => x.AppFamilyId,
                        principalTable: "app_families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_app_families_Key",
                table: "app_families",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_app_family_mappings_AppFamilyId",
                table: "app_family_mappings",
                column: "AppFamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_app_family_mappings_MappingType_MatchKey",
                table: "app_family_mappings",
                columns: new[] { "MappingType", "MatchKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_device_state_sessions_DeviceId_ClientSessionId",
                table: "device_state_sessions",
                columns: new[] { "DeviceId", "ClientSessionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_family_mappings");

            migrationBuilder.DropTable(
                name: "device_state_sessions");

            migrationBuilder.DropTable(
                name: "app_families");
        }
    }
}
