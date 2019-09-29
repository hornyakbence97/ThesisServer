using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ThesisServer.Migrations
{
    public partial class AddFilesToNetwork : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "NetworkId",
                table: "VirtualFile",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_VirtualFile_NetworkId",
                table: "VirtualFile",
                column: "NetworkId");

            migrationBuilder.AddForeignKey(
                name: "ForeignKey_VirtualFileEntity_NetworkEntity",
                table: "VirtualFile",
                column: "NetworkId",
                principalTable: "Network",
                principalColumn: "NetworkId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "ForeignKey_VirtualFileEntity_NetworkEntity",
                table: "VirtualFile");

            migrationBuilder.DropIndex(
                name: "IX_VirtualFile_NetworkId",
                table: "VirtualFile");

            migrationBuilder.DropColumn(
                name: "NetworkId",
                table: "VirtualFile");
        }
    }
}
