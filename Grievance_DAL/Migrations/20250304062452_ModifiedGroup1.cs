using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grievance_DAL.Migrations
{
    public partial class ModifiedGroup1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HODofGroupId",
                table: "Groups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCommitee",
                table: "Groups",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHOD",
                table: "Groups",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HODofGroupId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "IsCommitee",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "IsHOD",
                table: "Groups");
        }
    }
}
