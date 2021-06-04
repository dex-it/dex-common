using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dex.Cap.Ef.Tests.Migrations
{
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "_last_operation",
                columns: table => new
                {
                    IdempotentKey = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__last_operation", x => x.IdempotentKey);
                });

            migrationBuilder.CreateTable(
                name: "_outbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Retries = table.Column<int>(type: "integer", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true),
                    Updated = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    OutboxMessageType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__outbox", x => x.Id);
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
                name: "IX__last_operation_Created",
                table: "_last_operation",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX__outbox_Created",
                table: "_outbox",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX__outbox_OutboxMessageType",
                table: "_outbox",
                column: "OutboxMessageType");

            migrationBuilder.CreateIndex(
                name: "IX__outbox_Retries",
                table: "_outbox",
                column: "Retries");

            migrationBuilder.CreateIndex(
                name: "IX__outbox_Status",
                table: "_outbox",
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
                name: "_last_operation");

            migrationBuilder.DropTable(
                name: "_outbox");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
