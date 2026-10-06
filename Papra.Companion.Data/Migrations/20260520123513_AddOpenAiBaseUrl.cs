using Microsoft.EntityFrameworkCore.Migrations;

namespace Papra.Companion.Data.Migrations;

public partial class AddOpenAiBaseUrl : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "OpenAiBaseUrl",
            table: "PipelineSettings",
            type: "TEXT",
            nullable: false,
            defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "OpenAiBaseUrl",
            table: "PipelineSettings");
    }
}
