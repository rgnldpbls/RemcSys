using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemcSys.Migrations.RemcDB
{
    /// <inheritdoc />
    public partial class addotherattriinsettingsmodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isEFRApplication",
                table: "Settings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "isUFRApplication",
                table: "Settings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "isUFRLApplication",
                table: "Settings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isEFRApplication",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "isUFRApplication",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "isUFRLApplication",
                table: "Settings");
        }
    }
}
