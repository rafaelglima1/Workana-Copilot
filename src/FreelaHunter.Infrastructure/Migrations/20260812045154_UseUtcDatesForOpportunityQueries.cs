using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreelaHunter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UseUtcDatesForOpportunityQueries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CapturedAtUtc",
                table: "Opportunities",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAtUtc",
                table: "Opportunities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_CapturedAtUtc",
                table: "Opportunities",
                column: "CapturedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_Marketplace",
                table: "Opportunities",
                column: "Marketplace");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_PublishedAtUtc",
                table: "Opportunities",
                column: "PublishedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_Status",
                table: "Opportunities",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Opportunities_CapturedAtUtc",
                table: "Opportunities");

            migrationBuilder.DropIndex(
                name: "IX_Opportunities_Marketplace",
                table: "Opportunities");

            migrationBuilder.DropIndex(
                name: "IX_Opportunities_PublishedAtUtc",
                table: "Opportunities");

            migrationBuilder.DropIndex(
                name: "IX_Opportunities_Status",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "CapturedAtUtc",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "PublishedAtUtc",
                table: "Opportunities");
        }
    }
}
