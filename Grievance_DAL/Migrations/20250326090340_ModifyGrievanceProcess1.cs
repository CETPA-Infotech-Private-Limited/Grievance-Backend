using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grievance_DAL.Migrations
{
    public partial class ModifyGrievanceProcess1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVisited",
                table: "GrievanceProcesses",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVisited",
                table: "GrievanceProcesses");
        }
    }
}
