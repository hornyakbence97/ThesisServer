using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ThesisServer.Migrations
{
    public partial class NetworkEntityChangeHashToByteArrayRemoveString : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NetworkPasswordHash",
                table: "Network");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "NetworkPasswordHash",
                table: "Network",
                nullable: true);
        }
    }
}
