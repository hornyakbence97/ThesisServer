using Microsoft.EntityFrameworkCore.Migrations;

namespace ThesisServer.Migrations
{
    public partial class UserEntityAddMaxSpace : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxSpace",
                table: "User",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxSpace",
                table: "User");
        }
    }
}
