using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ThesisServer.Migrations
{
    public partial class VirtualFileEntityCreatin : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VirtualFile",
                columns: table => new
                {
                    FileId = table.Column<Guid>(nullable: false),
                    FileName = table.Column<string>(nullable: true),
                    FileSize = table.Column<long>(nullable: false),
                    LastModified = table.Column<DateTime>(nullable: false),
                    UploadedBy = table.Column<Guid>(nullable: false),
                    MimeType = table.Column<string>(nullable: true),
                    Created = table.Column<DateTime>(nullable: false),
                    ModifiedBy = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualFile", x => x.FileId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VirtualFile");
        }
    }
}
