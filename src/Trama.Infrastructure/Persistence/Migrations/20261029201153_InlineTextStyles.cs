using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trama.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InlineTextStyles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NotebookTextStyles",
            columns: table => new
            {
                PageId = table.Column<Guid>(type: "TEXT", nullable: false),
                BlockId = table.Column<Guid>(type: "TEXT", nullable: false),
                Styles = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NotebookTextStyles", x => new { x.PageId, x.BlockId });
                table.ForeignKey(
                    name: "FK_NotebookTextStyles_NotebookBlocks_PageId_BlockId",
                    columns: x => new { x.PageId, x.BlockId },
                    principalTable: "NotebookBlocks",
                    principalColumns: new[] { "PageId", "Id" },
                    onDelete: ReferentialAction.Cascade);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "NotebookTextStyles");
    }
}
