using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemcSys.Migrations.RemcDB
{
    /// <inheritdoc />
    public partial class addboolremindinevaluator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "reminded_OneDayBefore",
                table: "Evaluations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "reminded_OneDayOverdue",
                table: "Evaluations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "reminded_SevenDaysOverdue",
                table: "Evaluations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "reminded_ThreeDaysBefore",
                table: "Evaluations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "reminded_ThreeDaysOverdue",
                table: "Evaluations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "reminded_Today",
                table: "Evaluations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reminded_OneDayBefore",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "reminded_OneDayOverdue",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "reminded_SevenDaysOverdue",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "reminded_ThreeDaysBefore",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "reminded_ThreeDaysOverdue",
                table: "Evaluations");

            migrationBuilder.DropColumn(
                name: "reminded_Today",
                table: "Evaluations");
        }
    }
}
