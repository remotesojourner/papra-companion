using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Papra.Companion.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPromptFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OcrPrompt",
                table: "PipelineSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TagPrompt",
                table: "PipelineSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TitlePrompt",
                table: "PipelineSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OcrPrompt",
                table: "PipelineSettings");

            migrationBuilder.DropColumn(
                name: "TagPrompt",
                table: "PipelineSettings");

            migrationBuilder.DropColumn(
                name: "TitlePrompt",
                table: "PipelineSettings");
        }
    }
}
