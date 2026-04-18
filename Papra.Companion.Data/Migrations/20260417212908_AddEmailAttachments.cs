using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Papra.Companion.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailAttachmentLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MessageId = table.Column<string>(type: "TEXT", nullable: false),
                    AttachmentName = table.Column<string>(type: "TEXT", nullable: false),
                    SavedPath = table.Column<string>(type: "TEXT", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", nullable: false),
                    FromEmail = table.Column<string>(type: "TEXT", nullable: false),
                    MessageDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DownloadedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Succeeded = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAttachmentLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailAttachmentSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Host = table.Column<string>(type: "TEXT", nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    ImapFolder = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectRegex = table.Column<string>(type: "TEXT", nullable: false),
                    SubjectRegexIgnoreCase = table.Column<bool>(type: "INTEGER", nullable: false),
                    SubjectRegexMatchAnywhere = table.Column<bool>(type: "INTEGER", nullable: false),
                    OutputFolder = table.Column<string>(type: "TEXT", nullable: false),
                    FilenameTemplate = table.Column<string>(type: "TEXT", nullable: false),
                    UseSsl = table.Column<bool>(type: "INTEGER", nullable: false),
                    UseStartTls = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeleteAfterDownload = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeleteCopyFolder = table.Column<string>(type: "TEXT", nullable: false),
                    PollIntervalSeconds = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAttachmentSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachmentLog_DownloadedAt",
                table: "EmailAttachmentLog",
                column: "DownloadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachmentLog_MessageId_AttachmentName",
                table: "EmailAttachmentLog",
                columns: new[] { "MessageId", "AttachmentName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailAttachmentLog");

            migrationBuilder.DropTable(
                name: "EmailAttachmentSettings");
        }
    }
}
