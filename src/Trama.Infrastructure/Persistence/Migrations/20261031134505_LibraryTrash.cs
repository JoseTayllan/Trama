using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trama.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class LibraryTrash : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LibraryTrash",
            columns: table => new
            {
                Kind = table.Column<int>(type: "INTEGER", nullable: false),
                EntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                BatchId = table.Column<Guid>(type: "TEXT", nullable: false),
                IsRoot = table.Column<bool>(type: "INTEGER", nullable: false),
                Title = table.Column<string>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LibraryTrash", x => new { x.Kind, x.EntryId });
            });

        migrationBuilder.CreateIndex(
            name: "IX_LibraryTrash_BatchId",
            table: "LibraryTrash",
            column: "BatchId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "LibraryTrash");
    }
}
