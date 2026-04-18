using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Papra.Companion.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOutputFolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OutputFolder",
                table: "EmailAttachmentSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OutputFolder",
                table: "EmailAttachmentSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
