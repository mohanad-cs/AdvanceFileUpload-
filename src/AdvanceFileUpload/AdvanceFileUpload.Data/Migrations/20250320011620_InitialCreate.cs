using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdvanceFileUpload.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileUploadSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CompressionAlgorithm = table.Column<int>(type: "int", nullable: true),
                    CompressionLevel = table.Column<int>(type: "int", nullable: true),
                    SavingDirectory = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    CompressedFileSize = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UploadDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SessionStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SessionEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaxChunkSize = table.Column<long>(type: "bigint", nullable: false),
                    CurrentHubConnectionId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileUploadSession", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChunkFiles",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChunkIndex = table.Column<int>(type: "int", nullable: false),
                    ChunkPath = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ChunkSize = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChunkFile", x => new { x.SessionId, x.ChunkIndex });
                    table.ForeignKey(
                        name: "FK_FileUploadSession_ChunkFiles",
                        column: x => x.SessionId,
                        principalTable: "FileUploadSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChunkFiles");

            migrationBuilder.DropTable(
                name: "FileUploadSessions");
        }
    }
}
