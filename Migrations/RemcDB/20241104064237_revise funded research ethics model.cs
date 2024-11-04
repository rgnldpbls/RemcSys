using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RemcSys.Migrations.RemcDB
{
    /// <inheritdoc />
    public partial class revisefundedresearchethicsmodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "completionCertificate_Id",
                table: "FundedResearchEthics");

            migrationBuilder.DropColumn(
                name: "ethicClearance_Id",
                table: "FundedResearchEthics");

            migrationBuilder.RenameColumn(
                name: "urec_No",
                table: "FundedResearchEthics",
                newName: "file_Feedback");

            migrationBuilder.AddColumn<byte[]>(
                name: "data",
                table: "FundedResearchEthics",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "file_Name",
                table: "FundedResearchEthics",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "file_Status",
                table: "FundedResearchEthics",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "file_Type",
                table: "FundedResearchEthics",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "file_Uploaded",
                table: "FundedResearchEthics",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "data",
                table: "FundedResearchEthics");

            migrationBuilder.DropColumn(
                name: "file_Name",
                table: "FundedResearchEthics");

            migrationBuilder.DropColumn(
                name: "file_Status",
                table: "FundedResearchEthics");

            migrationBuilder.DropColumn(
                name: "file_Type",
                table: "FundedResearchEthics");

            migrationBuilder.DropColumn(
                name: "file_Uploaded",
                table: "FundedResearchEthics");

            migrationBuilder.RenameColumn(
                name: "file_Feedback",
                table: "FundedResearchEthics",
                newName: "urec_No");

            migrationBuilder.AddColumn<int>(
                name: "completionCertificate_Id",
                table: "FundedResearchEthics",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ethicClearance_Id",
                table: "FundedResearchEthics",
                type: "int",
                nullable: true);
        }
    }
}
