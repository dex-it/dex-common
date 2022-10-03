using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dex.Cap.Ef.Tests.Migrations
{
    public partial class AddCorrId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
