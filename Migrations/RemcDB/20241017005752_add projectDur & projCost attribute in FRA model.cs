using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemcSys.Migrations.RemcDB
{
    /// <inheritdoc />
    public partial class addprojectDurprojCostattributeinFRAmodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "project_Duration",
                table: "FundedResearchApplication",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "total_project_Cost",
                table: "FundedResearchApplication",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "project_Duration",
                table: "FundedResearchApplication");

            migrationBuilder.DropColumn(
                name: "total_project_Cost",
                table: "FundedResearchApplication");
        }
    }
}
