using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dex.Cap.Ef.Tests.Migrations
{
    public partial class OnceExecuterStringKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "IdempotentKey",
                schema: "cap",
                table: "last_transaction",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "IdempotentKey",
                schema: "cap",
                table: "last_transaction",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
