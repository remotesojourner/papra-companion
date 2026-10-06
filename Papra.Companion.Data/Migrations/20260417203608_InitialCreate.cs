using Microsoft.EntityFrameworkCore.Migrations;

namespace Papra.Companion.Data.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "JobResults",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                DocumentId = table.Column<string>(type: "TEXT", nullable: false),
                OrganizationId = table.Column<string>(type: "TEXT", nullable: false),
                StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                Status = table.Column<string>(type: "TEXT", nullable: false),
                ExtractedTitle = table.Column<string>(type: "TEXT", nullable: true),
                ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_JobResults", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_JobResults_StartedAt",
            table: "JobResults",
            column: "StartedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "JobResults");
    }
}
