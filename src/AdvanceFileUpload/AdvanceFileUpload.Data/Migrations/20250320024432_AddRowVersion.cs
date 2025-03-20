using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdvanceFileUpload.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeStamp",
                table: "FileUploadSessions");

            migrationBuilder.AddColumn<byte[]>(
                name: "Version",
                table: "FileUploadSessions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "FileUploadSessions");

            migrationBuilder.AddColumn<byte[]>(
                name: "TimeStamp",
                table: "FileUploadSessions",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
