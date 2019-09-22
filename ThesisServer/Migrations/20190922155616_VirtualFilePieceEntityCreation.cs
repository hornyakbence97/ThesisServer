using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ThesisServer.Migrations
{
    public partial class VirtualFilePieceEntityCreation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VirtualFilePiece",
                columns: table => new
                {
                    FilePieceId = table.Column<Guid>(nullable: false),
                    FilePieceSize = table.Column<long>(nullable: false),
                    FileId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VirtualFilePiece", x => x.FilePieceId);
                    table.ForeignKey(
                        name: "ForeignKey_VirtualFilePieceEntity_VirtualFileEntity",
                        column: x => x.FileId,
                        principalTable: "VirtualFile",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VirtualFilePiece_FileId",
                table: "VirtualFilePiece",
                column: "FileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VirtualFilePiece");
        }
    }
}
