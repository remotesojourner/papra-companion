using Microsoft.EntityFrameworkCore.Migrations;

namespace Papra.Companion.Data.Migrations;

public partial class RemoveOcrTagsMistralAddProcessingDelay : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MistralApiKey",
            table: "PipelineSettings");

        migrationBuilder.DropColumn(
            name: "OcrPrompt",
            table: "PipelineSettings");

        migrationBuilder.DropColumn(
            name: "TagPrompt",
            table: "PipelineSettings");

        migrationBuilder.AddColumn<int>(
            name: "ProcessingDelaySeconds",
            table: "PipelineSettings",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ProcessingDelaySeconds",
            table: "PipelineSettings");

        migrationBuilder.AddColumn<string>(
            name: "MistralApiKey",
            table: "PipelineSettings",
            type: "TEXT",
            nullable: false,
            defaultValue: "");

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
    }
}
