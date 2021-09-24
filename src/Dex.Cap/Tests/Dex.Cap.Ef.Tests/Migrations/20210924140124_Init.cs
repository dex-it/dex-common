using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dex.Cap.Ef.Tests.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cap");

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

            migrationBuilder.CreateTable(
                name: "outbox",
                schema: "cap",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageType = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ActivityId = table.Column<string>(type: "text", nullable: true),
                    Retries = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Updated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LockTimeout = table.Column<TimeSpan>(type: "interval", nullable: false, defaultValue: new TimeSpan(0, 0, 0, 30, 0), comment: "Maximum allowable blocking time"),
                    LockId = table.Column<Guid>(type: "uuid", nullable: true, comment: "Idempotency key (unique key of the thread that captured the lock)"),
                    LockExpirationTimeUtc = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, comment: "Preventive timeout (maximum lifetime of actuality 'LockId')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Years = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_last_transaction_Created",
                schema: "cap",
                table: "last_transaction",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_CreatedUtc",
                schema: "cap",
                table: "outbox",
                column: "CreatedUtc");

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

            migrationBuilder.CreateIndex(
                name: "IX_Users_Name",
                table: "Users",
                column: "Name",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "last_transaction",
                schema: "cap");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "cap");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
