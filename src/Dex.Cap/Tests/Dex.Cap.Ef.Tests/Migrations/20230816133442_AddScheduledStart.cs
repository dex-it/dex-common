using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dex.Cap.Ef.Tests.Migrations
{
    public partial class AddScheduledStart : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledStartIndexing",
                schema: "cap",
                table: "outbox",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartAtUtc",
                schema: "cap",
                table: "outbox",
                type: "timestamp without time zone",
                nullable: true);
            
            migrationBuilder.Sql("UPDATE outbox set StartAtUtc = now(), ScheduledStartIndexing = now() WHERE Status <> 2;");
            
            migrationBuilder.CreateIndex(
                name: "IX_outbox_ScheduledStartIndexing",
                schema: "cap",
                table: "outbox",
                column: "ScheduledStartIndexing");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_ScheduledStartIndexing",
                schema: "cap",
                table: "outbox");

            migrationBuilder.DropColumn(
                name: "ScheduledStartIndexing",
                schema: "cap",
                table: "outbox");

            migrationBuilder.DropColumn(
                name: "StartAtUtc",
                schema: "cap",
                table: "outbox");
        }
    }
}
