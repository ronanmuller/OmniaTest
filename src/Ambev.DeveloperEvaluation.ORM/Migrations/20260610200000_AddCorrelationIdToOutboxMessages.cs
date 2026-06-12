using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ambev.DeveloperEvaluation.ORM.Migrations
{
    /// <inheritdoc />
    public partial class AddCorrelationIdToOutboxMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValueSql keeps existing rows valid — they get a random UUID instead of null/error.
            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                table: "outbox_messages",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_CorrelationId",
                table: "outbox_messages",
                column: "CorrelationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_CorrelationId",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "outbox_messages");
        }
    }
}
