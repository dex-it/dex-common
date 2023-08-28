using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dex.Cap.Ef.Tests.Migrations
{
    public partial class ChangeOutboxEnvelopeIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_Retries",
                schema: "cap",
                table: "outbox");

            migrationBuilder.DropIndex(
                name: "IX_outbox_ScheduledStartIndexing",
                schema: "cap",
                table: "outbox");

            migrationBuilder.DropIndex(
                name: "IX_outbox_Status",
                schema: "cap",
                table: "outbox");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_ScheduledStartIndexing_Status_Retries",
                schema: "cap",
                table: "outbox",
                columns: new[] { "ScheduledStartIndexing", "Status", "Retries" },
                filter: "\"Status\" in (0,1)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_ScheduledStartIndexing_Status_Retries",
                schema: "cap",
                table: "outbox");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_Retries",
                schema: "cap",
                table: "outbox",
                column: "Retries");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_ScheduledStartIndexing",
                schema: "cap",
                table: "outbox",
                column: "ScheduledStartIndexing");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_Status",
                schema: "cap",
                table: "outbox",
                column: "Status");
        }
    }
}
