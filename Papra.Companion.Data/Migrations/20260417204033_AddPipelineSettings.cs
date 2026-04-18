using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Papra.Companion.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPipelineSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PipelineSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PapraBaseUrl = table.Column<string>(type: "TEXT", nullable: false),
                    PapraApiToken = table.Column<string>(type: "TEXT", nullable: false),
                    MistralApiKey = table.Column<string>(type: "TEXT", nullable: false),
                    OpenAiApiKey = table.Column<string>(type: "TEXT", nullable: false),
                    OpenAiModel = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipelineSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PipelineSettings");
        }
    }
}
