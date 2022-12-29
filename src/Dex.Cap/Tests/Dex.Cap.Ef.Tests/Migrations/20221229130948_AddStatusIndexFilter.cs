using Microsoft.EntityFrameworkCore.Migrations;

namespace Dex.Cap.Ef.Tests.Migrations
{
    public partial class AddStatusIndexFilter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_Status",
                schema: "cap",
                table: "outbox");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_Status",
                schema: "cap",
                table: "outbox",
                column: "Status",
                filter: "\"Status\" in (0,1)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_Status",
                schema: "cap",
                table: "outbox");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_Status",
                schema: "cap",
                table: "outbox",
                column: "Status");
        }
    }
}
