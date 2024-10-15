using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemcSys.Migrations.RemcDB
{
    /// <inheritdoc />
    public partial class addisArchivedattributeinFRAmodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isArchive",
                table: "FundedResearchApplication",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isArchive",
                table: "FundedResearchApplication");
        }
    }
}
