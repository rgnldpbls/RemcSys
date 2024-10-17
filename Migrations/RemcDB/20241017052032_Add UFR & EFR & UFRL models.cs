using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemcSys.Migrations.RemcDB
{
    /// <inheritdoc />
    public partial class AddUFREFRUFRLmodels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternallyFundedResearches",
                columns: table => new
                {
                    efrw_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    research_Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    team_Leader = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    teamLead_Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    team_Members = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    college = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    branch = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    field_of_Study = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    research_Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    start_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    projectDuration = table.Column<int>(type: "int", nullable: true),
                    dts_No = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    project_Duration = table.Column<int>(type: "int", nullable: false),
                    total_project_Cost = table.Column<double>(type: "float", nullable: true),
                    fra_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    isArchive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternallyFundedResearches", x => x.efrw_Id);
                    table.ForeignKey(
                        name: "FK_ExternallyFundedResearches_FundedResearchApplication_fra_Id",
                        column: x => x.fra_Id,
                        principalTable: "FundedResearchApplication",
                        principalColumn: "fra_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UniversityFundedResearches",
                columns: table => new
                {
                    ufrw_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    research_Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    team_Leader = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    teamLead_Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    team_Members = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    college = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    branch = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    field_of_Study = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    research_Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    start_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    projectDuration = table.Column<int>(type: "int", nullable: true),
                    dts_No = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    project_Duration = table.Column<int>(type: "int", nullable: false),
                    total_project_Cost = table.Column<double>(type: "float", nullable: true),
                    fra_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    isArchive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UniversityFundedResearches", x => x.ufrw_Id);
                    table.ForeignKey(
                        name: "FK_UniversityFundedResearches_FundedResearchApplication_fra_Id",
                        column: x => x.fra_Id,
                        principalTable: "FundedResearchApplication",
                        principalColumn: "fra_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UniversityFundedResearchLoads",
                columns: table => new
                {
                    ufrl_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    research_Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    team_Leader = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    teamLead_Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    team_Members = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    college = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    branch = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    field_of_Study = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    research_Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    start_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    projectDuration = table.Column<int>(type: "int", nullable: true),
                    dts_No = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    project_Duration = table.Column<int>(type: "int", nullable: false),
                    total_project_Cost = table.Column<double>(type: "float", nullable: true),
                    fra_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    isArchive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UniversityFundedResearchLoads", x => x.ufrl_Id);
                    table.ForeignKey(
                        name: "FK_UniversityFundedResearchLoads_FundedResearchApplication_fra_Id",
                        column: x => x.fra_Id,
                        principalTable: "FundedResearchApplication",
                        principalColumn: "fra_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternallyFundedResearches_fra_Id",
                table: "ExternallyFundedResearches",
                column: "fra_Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UniversityFundedResearches_fra_Id",
                table: "UniversityFundedResearches",
                column: "fra_Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UniversityFundedResearchLoads_fra_Id",
                table: "UniversityFundedResearchLoads",
                column: "fra_Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternallyFundedResearches");

            migrationBuilder.DropTable(
                name: "UniversityFundedResearches");

            migrationBuilder.DropTable(
                name: "UniversityFundedResearchLoads");
        }
    }
}
