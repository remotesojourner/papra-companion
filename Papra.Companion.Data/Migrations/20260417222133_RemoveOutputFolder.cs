using Microsoft.EntityFrameworkCore.Migrations;

namespace Papra.Companion.Data.Migrations;

public partial class RemoveOutputFolder : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "OutputFolder",
            table: "EmailAttachmentSettings");
    }

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
