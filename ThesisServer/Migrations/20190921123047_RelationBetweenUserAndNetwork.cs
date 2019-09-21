using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ThesisServer.Migrations
{
    public partial class RelationBetweenUserAndNetwork : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "NetworkId",
                table: "User",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_NetworkId",
                table: "User",
                column: "NetworkId");

            migrationBuilder.AddForeignKey(
                name: "ForeignKey_UserEntity_NetworkEntity",
                table: "User",
                column: "NetworkId",
                principalTable: "Network",
                principalColumn: "NetworkId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "ForeignKey_UserEntity_NetworkEntity",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_User_NetworkId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "NetworkId",
                table: "User");
        }
    }
}
