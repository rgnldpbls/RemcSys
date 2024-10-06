using System;
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
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundedResearchApplication", x => x.fra_Id);
                });

            migrationBuilder.CreateTable(
                name: "ActionLogs",
                columns: table => new
                {
                    LogId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FraId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProjLead = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FraType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                name: "FileRequirement",
                columns: table => new
                {
                    fr_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    file_Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    file_Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    file_Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_ActionLogs_FraId",
                table: "ActionLogs",
                column: "FraId");

            migrationBuilder.CreateIndex(
                name: "IX_FileRequirement_fra_Id",
                table: "FileRequirement",
                column: "fra_Id");

            migrationBuilder.CreateIndex(
                name: "IX_FundedResearchEthics_fra_Id",
                table: "FundedResearchEthics",
                column: "fra_Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedForms_fra_Id",
                table: "GeneratedForms",
                column: "fra_Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionLogs");

            migrationBuilder.DropTable(
                name: "FileRequirement");

            migrationBuilder.DropTable(
                name: "FundedResearchEthics");

            migrationBuilder.DropTable(
                name: "GeneratedForms");

            migrationBuilder.DropTable(
                name: "FundedResearchApplication");
        }
    }
}
