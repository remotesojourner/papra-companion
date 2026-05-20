using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Papra.Companion.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenAiBaseUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OpenAiBaseUrl",
                table: "PipelineSettings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpenAiBaseUrl",
                table: "PipelineSettings");
        }
    }
}
