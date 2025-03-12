using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grievance_DAL.Migrations
{
    public partial class ModifiedGroupMst2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCommitee",
                table: "Groups");

            migrationBuilder.RenameColumn(
                name: "IsHOD",
                table: "Groups",
                newName: "IsRoleGroup");

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "Groups",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Groups");

            migrationBuilder.RenameColumn(
                name: "IsRoleGroup",
                table: "Groups",
                newName: "IsHOD");

            migrationBuilder.AddColumn<bool>(
                name: "IsCommitee",
                table: "Groups",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
