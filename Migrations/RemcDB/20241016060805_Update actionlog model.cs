using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemcSys.Migrations.RemcDB
{
    /// <inheritdoc />
    public partial class Updateactionlogmodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ActionLogs");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ActionLogs");

            migrationBuilder.RenameColumn(
                name: "ProjLead",
                table: "ActionLogs",
                newName: "ResearchType");

            migrationBuilder.RenameColumn(
                name: "FraType",
                table: "ActionLogs",
                newName: "Name");

            migrationBuilder.AddColumn<bool>(
                name: "isChief",
                table: "ActionLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "isEvaluator",
                table: "ActionLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "isTeamLeader",
                table: "ActionLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isChief",
                table: "ActionLogs");

            migrationBuilder.DropColumn(
                name: "isEvaluator",
                table: "ActionLogs");

            migrationBuilder.DropColumn(
                name: "isTeamLeader",
                table: "ActionLogs");

            migrationBuilder.RenameColumn(
                name: "ResearchType",
                table: "ActionLogs",
                newName: "ProjLead");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "ActionLogs",
                newName: "FraType");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ActionLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ActionLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
