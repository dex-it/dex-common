using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dex.Events.Distributed.Tests.Migrations
{
    public partial class AddCorrId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "last_transaction",
                schema: "cap");

            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                schema: "cap",
                table: "outbox",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_outbox_CorrelationId",
                schema: "cap",
                table: "outbox",
                column: "CorrelationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_CorrelationId",
                schema: "cap",
                table: "outbox");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                schema: "cap",
                table: "outbox");

            migrationBuilder.CreateTable(
                name: "last_transaction",
                schema: "cap",
                columns: table => new
                {
                    IdempotentKey = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_last_transaction", x => x.IdempotentKey);
                });

            migrationBuilder.CreateIndex(
                name: "IX_last_transaction_Created",
                schema: "cap",
                table: "last_transaction",
                column: "Created");
        }
    }
}
