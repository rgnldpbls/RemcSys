using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemcSys.Migrations.RemcDB
{
    /// <inheritdoc />
    public partial class AddFundedResearchApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FundedResearchApplication",
                columns: table => new
                {
                    fra_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    fra_Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    research_Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    applicant_Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    applicant_Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    college = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    branch = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    field_of_Study = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    application_Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    submission_Date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    dts_No = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundedResearchApplication", x => x.fra_Id);
                });

            migrationBuilder.CreateTable(
                name: "FileRequirement",
                columns: table => new
                {
                    fr_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    file_Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    file_Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    file_Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    file_Uploaded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    fra_Id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FundedResearchApplicationfra_Id = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileRequirement", x => x.fr_Id);
                    table.ForeignKey(
                        name: "FK_FileRequirement_FundedResearchApplication_FundedResearchApplicationfra_Id",
                        column: x => x.FundedResearchApplicationfra_Id,
                        principalTable: "FundedResearchApplication",
                        principalColumn: "fra_Id");
                });

            migrationBuilder.CreateTable(
                name: "ResearchStaff",
                columns: table => new
                {
                    rs_Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    rs_Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    rs_Role = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fra_Id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FundedResearchApplicationfra_Id = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResearchStaff", x => x.rs_Id);
                    table.ForeignKey(
                        name: "FK_ResearchStaff_FundedResearchApplication_FundedResearchApplicationfra_Id",
                        column: x => x.FundedResearchApplicationfra_Id,
                        principalTable: "FundedResearchApplication",
                        principalColumn: "fra_Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileRequirement_FundedResearchApplicationfra_Id",
                table: "FileRequirement",
                column: "FundedResearchApplicationfra_Id");

            migrationBuilder.CreateIndex(
                name: "IX_ResearchStaff_FundedResearchApplicationfra_Id",
                table: "ResearchStaff",
                column: "FundedResearchApplicationfra_Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileRequirement");

            migrationBuilder.DropTable(
                name: "ResearchStaff");

            migrationBuilder.DropTable(
                name: "FundedResearchApplication");
        }
    }
}
