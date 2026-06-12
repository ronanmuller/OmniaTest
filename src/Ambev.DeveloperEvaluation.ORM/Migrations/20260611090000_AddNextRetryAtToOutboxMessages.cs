using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ambev.DeveloperEvaluation.ORM.Migrations
{
    public partial class AddNextRetryAtToOutboxMessages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                table: "outbox_messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedAt_NextRetryAt_CreatedAt",
                table: "outbox_messages",
                columns: new[] { "ProcessedAt", "NextRetryAt", "CreatedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_ProcessedAt_NextRetryAt_CreatedAt",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                table: "outbox_messages");
        }
    }
}
