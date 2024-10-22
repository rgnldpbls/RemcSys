using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemcSys.Migrations.RemcDB
{
    /// <inheritdoc />
    public partial class addgenerateReportgenerateGAWADNomineesGAWADWinnersmodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "total_project_Cost",
                table: "FundedResearchApplication",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.CreateTable(
                name: "GAWADWinners",
                columns: table => new
                {
                    gw_Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    gw_fileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    gw_Data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    gn_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    file_Uploaded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                    gn_Data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    gn_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    generateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    gr_Data = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    gr_startDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    gr_endDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    gr_typeofReport = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    generateDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    isArchived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenerateReports", x => x.gr_Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GAWADWinners");

            migrationBuilder.DropTable(
                name: "GenerateGAWADNominees");

            migrationBuilder.DropTable(
                name: "GenerateReports");

            migrationBuilder.AlterColumn<double>(
                name: "total_project_Cost",
                table: "FundedResearchApplication",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);
        }
    }
}
