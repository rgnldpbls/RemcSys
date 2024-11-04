using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemcSys.Migrations.RemcDB
{
    /// <inheritdoc />
    public partial class revisefundedresearchethicsmodel2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "data",
                table: "FundedResearchEthics");

            migrationBuilder.AddColumn<byte[]>(
                name: "clearanceFile",
                table: "FundedResearchEthics",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "urecNo",
                table: "FundedResearchEthics",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "clearanceFile",
                table: "FundedResearchEthics");

            migrationBuilder.DropColumn(
                name: "urecNo",
                table: "FundedResearchEthics");

            migrationBuilder.AddColumn<byte[]>(
                name: "data",
                table: "FundedResearchEthics",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
