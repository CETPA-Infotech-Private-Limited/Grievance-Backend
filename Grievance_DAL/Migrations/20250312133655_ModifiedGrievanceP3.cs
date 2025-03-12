using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grievance_DAL.Migrations
{
    public partial class ModifiedGrievanceP3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TDepartment",
                table: "GrievanceProcesses",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TDepartment",
                table: "GrievanceProcesses");
        }
    }
}
