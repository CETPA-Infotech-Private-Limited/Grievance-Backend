using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grievance_DAL.Migrations
{
    public partial class GroupMstModified1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GrievanceMasters_Groups_ServiceId",
                table: "GrievanceMasters");

            migrationBuilder.DropForeignKey(
                name: "FK_GrievanceProcesses_Groups_ServiceId",
                table: "GrievanceProcesses");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropIndex(
                name: "IX_GrievanceProcesses_ServiceId",
                table: "GrievanceProcesses");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "GrievanceProcesses");

            migrationBuilder.RenameColumn(
                name: "HODofGroupId",
                table: "Groups",
                newName: "ParentGroupId");

            migrationBuilder.RenameColumn(
                name: "ServiceId",
                table: "GrievanceMasters",
                newName: "GroupId");

            migrationBuilder.RenameIndex(
                name: "IX_GrievanceMasters_ServiceId",
                table: "GrievanceMasters",
                newName: "IX_GrievanceMasters_GroupId");

            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "UserDepartmentMappings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsServiceCategory",
                table: "Groups",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UnitId",
                table: "Groups",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TGroupId",
                table: "GrievanceProcesses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TUnitId",
                table: "GrievanceProcesses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDepartmentMappings_GroupId",
                table: "UserDepartmentMappings",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Groups_ParentGroupId",
                table: "Groups",
                column: "ParentGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_GrievanceMasters_Groups_GroupId",
                table: "GrievanceMasters",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Groups_ParentGroupId",
                table: "Groups",
                column: "ParentGroupId",
                principalTable: "Groups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserDepartmentMappings_Groups_GroupId",
                table: "UserDepartmentMappings",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GrievanceMasters_Groups_GroupId",
                table: "GrievanceMasters");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Groups_ParentGroupId",
                table: "Groups");

            migrationBuilder.DropForeignKey(
                name: "FK_UserDepartmentMappings_Groups_GroupId",
                table: "UserDepartmentMappings");

            migrationBuilder.DropIndex(
                name: "IX_UserDepartmentMappings_GroupId",
                table: "UserDepartmentMappings");

            migrationBuilder.DropIndex(
                name: "IX_Groups_ParentGroupId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "UserDepartmentMappings");

            migrationBuilder.DropColumn(
                name: "IsServiceCategory",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "TGroupId",
                table: "GrievanceProcesses");

            migrationBuilder.DropColumn(
                name: "TUnitId",
                table: "GrievanceProcesses");

            migrationBuilder.RenameColumn(
                name: "ParentGroupId",
                table: "Groups",
                newName: "HODofGroupId");

            migrationBuilder.RenameColumn(
                name: "GroupId",
                table: "GrievanceMasters",
                newName: "ServiceId");

            migrationBuilder.RenameIndex(
                name: "IX_GrievanceMasters_GroupId",
                table: "GrievanceMasters",
                newName: "IX_GrievanceMasters_ServiceId");

            migrationBuilder.AddColumn<int>(
                name: "ServiceId",
                table: "GrievanceProcesses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupMasterId = table.Column<int>(type: "int", nullable: true),
                    ParentServiceId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true),
                    ModifyBy = table.Column<int>(type: "int", nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Services_Groups_GroupMasterId",
                        column: x => x.GroupMasterId,
                        principalTable: "Groups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Services_Services_ParentServiceId",
                        column: x => x.ParentServiceId,
                        principalTable: "Services",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GrievanceProcesses_ServiceId",
                table: "GrievanceProcesses",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_GroupMasterId",
                table: "Services",
                column: "GroupMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_ParentServiceId",
                table: "Services",
                column: "ParentServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_GrievanceMasters_Groups_ServiceId",
                table: "GrievanceMasters",
                column: "ServiceId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GrievanceProcesses_Groups_ServiceId",
                table: "GrievanceProcesses",
                column: "ServiceId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
