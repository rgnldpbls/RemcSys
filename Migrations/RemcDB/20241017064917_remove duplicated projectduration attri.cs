using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemcSys.Migrations.RemcDB
{
    /// <inheritdoc />
    public partial class removeduplicatedprojectdurationattri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "projectDuration",
                table: "UniversityFundedResearchLoads");

            migrationBuilder.DropColumn(
                name: "projectDuration",
                table: "UniversityFundedResearches");

            migrationBuilder.DropColumn(
                name: "projectDuration",
                table: "ExternallyFundedResearches");

            migrationBuilder.AlterColumn<double>(
                name: "total_project_Cost",
                table: "FundedResearchApplication",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "project_Duration",
                table: "FundedResearchApplication",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "projectDuration",
                table: "UniversityFundedResearchLoads",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "projectDuration",
                table: "UniversityFundedResearches",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "total_project_Cost",
                table: "FundedResearchApplication",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<int>(
                name: "project_Duration",
                table: "FundedResearchApplication",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "projectDuration",
                table: "ExternallyFundedResearches",
                type: "int",
                nullable: true);
        }
    }
}
