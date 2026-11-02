using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trama.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class KnowledgeCatalog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "KnowledgeSpaces",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Title = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_KnowledgeSpaces", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Notebooks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                SpaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                Title = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notebooks", x => x.Id);
                table.ForeignKey(
                    name: "FK_Notebooks_KnowledgeSpaces_SpaceId",
                    column: x => x.SpaceId,
                    principalTable: "KnowledgeSpaces",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PageLocations",
            columns: table => new
            {
                PageId = table.Column<Guid>(type: "TEXT", nullable: false),
                NotebookId = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PageLocations", x => x.PageId);
                table.ForeignKey(
                    name: "FK_PageLocations_Notebooks_NotebookId",
                    column: x => x.NotebookId,
                    principalTable: "Notebooks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Notebooks_SpaceId",
            table: "Notebooks",
            column: "SpaceId");

        migrationBuilder.CreateIndex(
            name: "IX_PageLocations_NotebookId",
            table: "PageLocations",
            column: "NotebookId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PageLocations");

        migrationBuilder.DropTable(
            name: "Notebooks");

        migrationBuilder.DropTable(
            name: "KnowledgeSpaces");
    }
}
