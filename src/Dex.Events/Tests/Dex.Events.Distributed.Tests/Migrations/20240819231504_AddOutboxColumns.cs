using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dex.Events.Distributed.Tests.Migrations
{
    public partial class AddOutboxColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_Retries",
                schema: "cap",
                table: "outbox");

            migrationBuilder.DropIndex(
                name: "IX_outbox_Status",
                schema: "cap",
                table: "outbox");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                schema: "cap",
                table: "outbox",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LockExpirationTimeUtc",
                schema: "cap",
                table: "outbox",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Preventive timeout (maximum lifetime of actuality 'LockId')",
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true,
                oldComment: "Preventive timeout (maximum lifetime of actuality 'LockId')");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedUtc",
                schema: "cap",
                table: "outbox",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledStartIndexing",
                schema: "cap",
                table: "outbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartAtUtc",
                schema: "cap",
                table: "outbox",
                type: "timestamp with time zone",
                nullable: true);

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

            migrationBuilder.DropColumn(
                name: "ScheduledStartIndexing",
                schema: "cap",
                table: "outbox");

            migrationBuilder.DropColumn(
                name: "StartAtUtc",
                schema: "cap",
                table: "outbox");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Updated",
                schema: "cap",
                table: "outbox",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LockExpirationTimeUtc",
                schema: "cap",
                table: "outbox",
                type: "timestamp without time zone",
                nullable: true,
                comment: "Preventive timeout (maximum lifetime of actuality 'LockId')",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Preventive timeout (maximum lifetime of actuality 'LockId')");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedUtc",
                schema: "cap",
                table: "outbox",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_Retries",
                schema: "cap",
                table: "outbox",
                column: "Retries");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_Status",
                schema: "cap",
                table: "outbox",
                column: "Status");
        }
    }
}
