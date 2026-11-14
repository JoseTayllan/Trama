using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trama.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class NotebookPageEdits : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NotebookPageEdits",
            columns: table => new
            {
                PageId = table.Column<Guid>(type: "TEXT", nullable: false),
                EditedAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NotebookPageEdits", x => x.PageId);
                table.ForeignKey(
                    name: "FK_NotebookPageEdits_NotebookPages_PageId",
                    column: x => x.PageId,
                    principalTable: "NotebookPages",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_NotebookPageEdits_EditedAtUtcTicks",
            table: "NotebookPageEdits",
            column: "EditedAtUtcTicks");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "NotebookPageEdits");
    }
}
