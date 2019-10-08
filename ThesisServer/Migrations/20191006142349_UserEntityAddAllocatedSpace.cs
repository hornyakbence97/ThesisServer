using Microsoft.EntityFrameworkCore.Migrations;

namespace ThesisServer.Migrations
{
    public partial class UserEntityAddAllocatedSpace : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "MaxSpace",
                table: "User",
                nullable: false,
                defaultValue: 300L,
                oldClrType: typeof(int),
                oldDefaultValue: 300);

            migrationBuilder.AddColumn<long>(
                name: "AllocatedSpace",
                table: "User",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllocatedSpace",
                table: "User");

            migrationBuilder.AlterColumn<int>(
                name: "MaxSpace",
                table: "User",
                nullable: false,
                defaultValue: 300,
                oldClrType: typeof(long),
                oldDefaultValue: 300L);
        }
    }
}
