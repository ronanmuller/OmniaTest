using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ambev.DeveloperEvaluation.ORM.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "outbox_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "processed_messages",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Consumer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_messages", x => new { x.MessageId, x.Consumer });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processed_messages");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "outbox_messages");
        }
    }
}
