using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ThesisServer.Migrations
{
    public partial class AddToken2ToUserEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Token",
                table: "User",
                newName: "Token1");

            migrationBuilder.AddColumn<Guid>(
                name: "Token2",
                table: "User",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_User_Token2",
                table: "User",
                column: "Token2");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_User_Token2",
                table: "User");

            migrationBuilder.DropColumn(
                name: "Token2",
                table: "User");

            migrationBuilder.RenameColumn(
                name: "Token1",
                table: "User",
                newName: "Token");
        }
    }
}
