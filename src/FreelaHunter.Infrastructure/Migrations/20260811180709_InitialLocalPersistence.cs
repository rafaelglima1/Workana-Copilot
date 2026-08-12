using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreelaHunter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialLocalPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FreelancerProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DefaultRateCurrency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreelancerProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Opportunities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Marketplace = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IntakeChannel = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ExternalReference = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", nullable: true),
                    Subcategory = table.Column<string>(type: "TEXT", nullable: true),
                    BudgetKind = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    BudgetBasis = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    BudgetAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    BudgetMinimum = table.Column<decimal>(type: "TEXT", nullable: true),
                    BudgetMaximum = table.Column<decimal>(type: "TEXT", nullable: true),
                    BudgetCurrency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    ClientDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    ClientCountry = table.Column<string>(type: "TEXT", nullable: true),
                    ClientPublishedProjects = table.Column<int>(type: "INTEGER", nullable: true),
                    ClientPaidProjects = table.Column<int>(type: "INTEGER", nullable: true),
                    ClientRating = table.Column<decimal>(type: "TEXT", nullable: true),
                    CompetitionProposalCount = table.Column<int>(type: "INTEGER", nullable: true),
                    CompetitionInterestedFreelancerCount = table.Column<int>(type: "INTEGER", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ClientDeadline = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    CapturedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Opportunities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FreelancerHourlyRates",
                columns: table => new
                {
                    FreelancerProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Currency = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreelancerHourlyRates", x => new { x.FreelancerProfileId, x.Currency });
                    table.ForeignKey(
                        name: "FK_FreelancerHourlyRates_FreelancerProfiles_FreelancerProfileId",
                        column: x => x.FreelancerProfileId,
                        principalTable: "FreelancerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FreelancerProfileSkills",
                columns: table => new
                {
                    FreelancerProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Level = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreelancerProfileSkills", x => new { x.FreelancerProfileId, x.Name });
                    table.ForeignKey(
                        name: "FK_FreelancerProfileSkills_FreelancerProfiles_FreelancerProfileId",
                        column: x => x.FreelancerProfileId,
                        principalTable: "FreelancerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpportunitySkills",
                columns: table => new
                {
                    OpportunityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Skill = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpportunitySkills", x => new { x.OpportunityId, x.Skill });
                    table.ForeignKey(
                        name: "FK_OpportunitySkills_Opportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "Opportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FreelancerHourlyRates");

            migrationBuilder.DropTable(
                name: "FreelancerProfileSkills");

            migrationBuilder.DropTable(
                name: "OpportunitySkills");

            migrationBuilder.DropTable(
                name: "FreelancerProfiles");

            migrationBuilder.DropTable(
                name: "Opportunities");
        }
    }
}
