using Microsoft.EntityFrameworkCore.Migrations;

namespace ThesisServer.Migrations
{
    public partial class UserEntityAddMaxSpaceDefaultValue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MaxSpace",
                table: "User",
                nullable: false,
                defaultValue: 300,
                oldClrType: typeof(int));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MaxSpace",
                table: "User",
                nullable: false,
                oldClrType: typeof(int),
                oldDefaultValue: 300);
        }
    }
}
