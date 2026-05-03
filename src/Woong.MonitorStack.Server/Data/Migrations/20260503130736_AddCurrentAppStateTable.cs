using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Woong.MonitorStack.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentAppStateTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "current_app_states",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientStateId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Platform = table.Column<int>(type: "integer", nullable: false),
                    PlatformAppKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TimezoneId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Source = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProcessId = table.Column<int>(type: "integer", nullable: true),
                    ProcessName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ProcessPath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    WindowHandle = table.Column<long>(type: "bigint", nullable: true),
                    WindowTitle = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_current_app_states", x => x.Id);
                    table.ForeignKey(
                        name: "FK_current_app_states_devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_current_app_states_DeviceId",
                table: "current_app_states",
                column: "DeviceId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "current_app_states");
        }
    }
}
