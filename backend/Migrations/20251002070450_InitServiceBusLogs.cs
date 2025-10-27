using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace servicebusapi2.Migrations
{
    /// <inheritdoc />
    public partial class InitServiceBusLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceBusLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceBusLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBusLogs_EventType",
                table: "ServiceBusLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBusLogs_Severity",
                table: "ServiceBusLogs",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceBusLogs_Timestamp",
                table: "ServiceBusLogs",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceBusLogs");
        }
    }
}
