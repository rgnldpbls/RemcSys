﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemcSys.Migrations.RemcDB
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Start = table.Column<DateTime>(type: "datetime2", nullable: false),
                    End = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Visibility = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Evaluator",
                columns: table => new
                {
                    evaluator_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    evaluator_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    evaluator_Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    field_of_Interest = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    center = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evaluator", x => x.evaluator_Id);
                });

            migrationBuilder.CreateTable(
                name: "FundedResearchApplication",
                columns: table => new
                {
                    fra_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    fra_Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    research_Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    applicant_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    applicant_Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    team_Members = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    college = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    branch = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    field_of_Study = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    application_Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    submission_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    dts_No = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    project_Duration = table.Column<int>(type: "int", nullable: false),
                    total_project_Cost = table.Column<double>(type: "float", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    isArchive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundedResearchApplication", x => x.fra_Id);
                });

            migrationBuilder.CreateTable(
                name: "GAWADWinners",
                columns: table => new
                {
                    gw_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    gw_fileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    gw_fileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    gw_Data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    file_Uploaded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GAWADWinners", x => x.gw_Id);
                });

            migrationBuilder.CreateTable(
                name: "GenerateGAWADNominees",
                columns: table => new
                {
                    gn_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    gn_fileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    gn_fileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    gn_Data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    gn_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    generateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    isArchived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenerateGAWADNominees", x => x.gn_Id);
                });

            migrationBuilder.CreateTable(
                name: "GenerateReports",
                columns: table => new
                {
                    gr_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    gr_fileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    gr_fileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    gr_Data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    gr_startDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    gr_endDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    gr_typeofReport = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    generateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    isArchived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenerateReports", x => x.gr_Id);
                });

            migrationBuilder.CreateTable(
                name: "ActionLogs",
                columns: table => new
                {
                    LogId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResearchType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    isTeamLeader = table.Column<bool>(type: "bit", nullable: false),
                    isChief = table.Column<bool>(type: "bit", nullable: false),
                    isEvaluator = table.Column<bool>(type: "bit", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FraId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionLogs", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_ActionLogs_FundedResearchApplication_FraId",
                        column: x => x.FraId,
                        principalTable: "FundedResearchApplication",
                        principalColumn: "fra_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Evaluations",
                columns: table => new
                {
                    evaluation_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    evaluation_Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    evaluator_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    evaluation_Grade = table.Column<double>(type: "float", nullable: true),
                    assigned_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    evaluation_Deadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    evaluation_Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    evaluator_Id = table.Column<int>(type: "int", nullable: false),
                    fra_Id = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evaluations", x => x.evaluation_Id);
                    table.ForeignKey(
                        name: "FK_Evaluations_Evaluator_evaluator_Id",
                        column: x => x.evaluator_Id,
                        principalTable: "Evaluator",
                        principalColumn: "evaluator_Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Evaluations_FundedResearchApplication_fra_Id",
                        column: x => x.fra_Id,
                        principalTable: "FundedResearchApplication",
                        principalColumn: "fra_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileRequirement",
                columns: table => new
                {
                    fr_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    file_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    file_Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    file_Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    document_Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    file_Feedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    file_Uploaded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fra_Id = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileRequirement", x => x.fr_Id);
                    table.ForeignKey(
                        name: "FK_FileRequirement_FundedResearchApplication_fra_Id",
                        column: x => x.fra_Id,
                        principalTable: "FundedResearchApplication",
                        principalColumn: "fra_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FundedResearches",
                columns: table => new
                {
                    fr_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    fr_Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    research_Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    team_Leader = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    teamLead_Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    team_Members = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    college = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    branch = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    field_of_Study = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    start_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    dts_No = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    project_Duration = table.Column<int>(type: "int", nullable: false),
                    total_project_Cost = table.Column<double>(type: "float", nullable: true),
                    isExtension1 = table.Column<bool>(type: "bit", nullable: false),
                    isExtension2 = table.Column<bool>(type: "bit", nullable: false),
                    fra_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    isArchive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundedResearches", x => x.fr_Id);
                    table.ForeignKey(
                        name: "FK_FundedResearches_FundedResearchApplication_fra_Id",
                        column: x => x.fra_Id,
                        principalTable: "FundedResearchApplication",
                        principalColumn: "fra_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FundedResearchEthics",
                columns: table => new
                {
                    fre_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    fra_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    urec_No = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ethicClearance_Id = table.Column<int>(type: "int", nullable: true),
                    completionCertificate_Id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundedResearchEthics", x => x.fre_Id);
                    table.ForeignKey(
                        name: "FK_FundedResearchEthics_FundedResearchApplication_fra_Id",
                        column: x => x.fra_Id,
                        principalTable: "FundedResearchApplication",
                        principalColumn: "fra_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeneratedForms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileContent = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fra_Id = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneratedForms_FundedResearchApplication_fra_Id",
                        column: x => x.fra_Id,
                        principalTable: "FundedResearchApplication",
                        principalColumn: "fra_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgressReports",
                columns: table => new
                {
                    pr_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    file_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    file_Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    file_Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    document_Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    file_Feedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    file_Uploaded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fr_Id = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressReports", x => x.pr_Id);
                    table.ForeignKey(
                        name: "FK_ProgressReports_FundedResearches_fr_Id",
                        column: x => x.fr_Id,
                        principalTable: "FundedResearches",
                        principalColumn: "fr_Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_FraId",
                table: "ActionLogs",
                column: "FraId");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_evaluator_Id",
                table: "Evaluations",
                column: "evaluator_Id");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_fra_Id",
                table: "Evaluations",
                column: "fra_Id");

            migrationBuilder.CreateIndex(
                name: "IX_FileRequirement_fra_Id",
                table: "FileRequirement",
                column: "fra_Id");

            migrationBuilder.CreateIndex(
                name: "IX_FundedResearches_fra_Id",
                table: "FundedResearches",
                column: "fra_Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundedResearchEthics_fra_Id",
                table: "FundedResearchEthics",
                column: "fra_Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedForms_fra_Id",
                table: "GeneratedForms",
                column: "fra_Id");

            migrationBuilder.CreateIndex(
                name: "IX_ProgressReports_fr_Id",
                table: "ProgressReports",
                column: "fr_Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionLogs");

            migrationBuilder.DropTable(
                name: "CalendarEvents");

            migrationBuilder.DropTable(
                name: "Evaluations");

            migrationBuilder.DropTable(
                name: "FileRequirement");

            migrationBuilder.DropTable(
                name: "FundedResearchEthics");

            migrationBuilder.DropTable(
                name: "GAWADWinners");

            migrationBuilder.DropTable(
                name: "GeneratedForms");

            migrationBuilder.DropTable(
                name: "GenerateGAWADNominees");

            migrationBuilder.DropTable(
                name: "GenerateReports");

            migrationBuilder.DropTable(
                name: "ProgressReports");

            migrationBuilder.DropTable(
                name: "Evaluator");

            migrationBuilder.DropTable(
                name: "FundedResearches");

            migrationBuilder.DropTable(
                name: "FundedResearchApplication");
        }
    }
}
