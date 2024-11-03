using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemcSys.Migrations.RemcDB
{
    /// <inheritdoc />
    public partial class addboolremindinprogressreport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "reminded_OneDayBefore",
                table: "FundedResearches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "reminded_OneDayOverdue",
                table: "FundedResearches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "reminded_SevenDaysOverdue",
                table: "FundedResearches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "reminded_ThreeDaysBefore",
                table: "FundedResearches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "reminded_ThreeDaysOverdue",
                table: "FundedResearches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "reminded_Today",
                table: "FundedResearches",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reminded_OneDayBefore",
                table: "FundedResearches");

            migrationBuilder.DropColumn(
                name: "reminded_OneDayOverdue",
                table: "FundedResearches");

            migrationBuilder.DropColumn(
                name: "reminded_SevenDaysOverdue",
                table: "FundedResearches");

            migrationBuilder.DropColumn(
                name: "reminded_ThreeDaysBefore",
                table: "FundedResearches");

            migrationBuilder.DropColumn(
                name: "reminded_ThreeDaysOverdue",
                table: "FundedResearches");

            migrationBuilder.DropColumn(
                name: "reminded_Today",
                table: "FundedResearches");
        }
    }
}
