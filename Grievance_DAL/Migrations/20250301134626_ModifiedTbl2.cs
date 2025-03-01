using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grievance_DAL.Migrations
{
    public partial class ModifiedTbl2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GrievanceMasters_Groups_GroupId",
                table: "GrievanceMasters");

            migrationBuilder.DropForeignKey(
                name: "FK_GrievanceMasters_Groups_GroupSubTypeId",
                table: "GrievanceMasters");

            migrationBuilder.DropForeignKey(
                name: "FK_GrievanceProcesses_Groups_GroupId",
                table: "GrievanceProcesses");

            migrationBuilder.DropForeignKey(
                name: "FK_GrievanceProcesses_Groups_GroupSubTypeId",
                table: "GrievanceProcesses");

            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Groups_ParentGroupId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_ParentGroupId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_GrievanceProcesses_GroupId",
                table: "GrievanceProcesses");

            migrationBuilder.DropIndex(
                name: "IX_GrievanceMasters_GroupSubTypeId",
                table: "GrievanceMasters");

            migrationBuilder.DropColumn(
                name: "ParentGroupId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "GrievanceProcesses");

            migrationBuilder.DropColumn(
                name: "GroupSubTypeId",
                table: "GrievanceMasters");

            migrationBuilder.RenameColumn(
                name: "GroupSubTypeId",
                table: "GrievanceProcesses",
                newName: "ServiceId");

            migrationBuilder.RenameIndex(
                name: "IX_GrievanceProcesses_GroupSubTypeId",
                table: "GrievanceProcesses",
                newName: "IX_GrievanceProcesses_ServiceId");

            migrationBuilder.RenameColumn(
                name: "GroupId",
                table: "GrievanceMasters",
                newName: "ServiceId");

            migrationBuilder.RenameIndex(
                name: "IX_GrievanceMasters_GroupId",
                table: "GrievanceMasters",
                newName: "IX_GrievanceMasters_ServiceId");

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ServiceDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentServiceId = table.Column<int>(type: "int", nullable: true),
                    GroupMasterId = table.Column<int>(type: "int", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifyBy = table.Column<int>(type: "int", nullable: true),
                    ModifyDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: true)
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GrievanceMasters_Groups_ServiceId",
                table: "GrievanceMasters");

            migrationBuilder.DropForeignKey(
                name: "FK_GrievanceProcesses_Groups_ServiceId",
                table: "GrievanceProcesses");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.RenameColumn(
                name: "ServiceId",
                table: "GrievanceProcesses",
                newName: "GroupSubTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_GrievanceProcesses_ServiceId",
                table: "GrievanceProcesses",
                newName: "IX_GrievanceProcesses_GroupSubTypeId");

            migrationBuilder.RenameColumn(
                name: "ServiceId",
                table: "GrievanceMasters",
                newName: "GroupId");

            migrationBuilder.RenameIndex(
                name: "IX_GrievanceMasters_ServiceId",
                table: "GrievanceMasters",
                newName: "IX_GrievanceMasters_GroupId");

            migrationBuilder.AddColumn<int>(
                name: "ParentGroupId",
                table: "Groups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "GrievanceProcesses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GroupSubTypeId",
                table: "GrievanceMasters",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_ParentGroupId",
                table: "Groups",
                column: "ParentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GrievanceProcesses_GroupId",
                table: "GrievanceProcesses",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GrievanceMasters_GroupSubTypeId",
                table: "GrievanceMasters",
                column: "GroupSubTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_GrievanceMasters_Groups_GroupId",
                table: "GrievanceMasters",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GrievanceMasters_Groups_GroupSubTypeId",
                table: "GrievanceMasters",
                column: "GroupSubTypeId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GrievanceProcesses_Groups_GroupId",
                table: "GrievanceProcesses",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GrievanceProcesses_Groups_GroupSubTypeId",
                table: "GrievanceProcesses",
                column: "GroupSubTypeId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Groups_ParentGroupId",
                table: "Groups",
                column: "ParentGroupId",
                principalTable: "Groups",
                principalColumn: "Id");
        }
    }
}
