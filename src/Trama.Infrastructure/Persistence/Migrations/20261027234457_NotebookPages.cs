using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trama.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class NotebookPages : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NotebookPages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Title = table.Column<string>(type: "TEXT", nullable: false),
                FormatVersion = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NotebookPages", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "NotebookBlocks",
            columns: table => new
            {
                PageId = table.Column<Guid>(type: "TEXT", nullable: false),
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Position = table.Column<int>(type: "INTEGER", nullable: false),
                Kind = table.Column<string>(type: "TEXT", nullable: false),
                Content = table.Column<string>(type: "TEXT", nullable: false),
                Language = table.Column<string>(type: "TEXT", nullable: false),
                AttachmentId = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NotebookBlocks", x => new { x.PageId, x.Id });
                table.ForeignKey(
                    name: "FK_NotebookBlocks_NotebookPages_PageId",
                    column: x => x.PageId,
                    principalTable: "NotebookPages",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_NotebookBlocks_PageId_Position",
            table: "NotebookBlocks",
            columns: new[] { "PageId", "Position" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "NotebookBlocks");

        migrationBuilder.DropTable(
            name: "NotebookPages");
    }
}
