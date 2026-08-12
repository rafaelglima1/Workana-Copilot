using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreelaHunter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLatestEvaluationSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OpportunityEvaluationSnapshots",
                columns: table => new
                {
                    OpportunityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileFingerprint = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    EvaluatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Evaluated = table.Column<bool>(type: "INTEGER", nullable: false),
                    Recommendation = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OverallScore = table.Column<decimal>(type: "TEXT", nullable: false),
                    PayloadSchemaVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", maxLength: 50000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpportunityEvaluationSnapshots", x => x.OpportunityId);
                    table.ForeignKey(
                        name: "FK_OpportunityEvaluationSnapshots_Opportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "Opportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityEvaluationSnapshots_EvaluatedAtUtc",
                table: "OpportunityEvaluationSnapshots",
                column: "EvaluatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityEvaluationSnapshots_OverallScore",
                table: "OpportunityEvaluationSnapshots",
                column: "OverallScore");

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityEvaluationSnapshots_Recommendation",
                table: "OpportunityEvaluationSnapshots",
                column: "Recommendation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OpportunityEvaluationSnapshots");
        }
    }
}
