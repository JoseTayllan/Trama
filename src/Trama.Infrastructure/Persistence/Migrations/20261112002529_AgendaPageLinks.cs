using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trama.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AgendaPageLinks : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AgendaPageLinks",
            columns: table => new
            {
                TaskId = table.Column<Guid>(type: "TEXT", nullable: false),
                PageId = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AgendaPageLinks", x => new { x.TaskId, x.PageId });
                table.ForeignKey(
                    name: "FK_AgendaPageLinks_AgendaTasks_TaskId",
                    column: x => x.TaskId,
                    principalTable: "AgendaTasks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AgendaPageLinks_PageId",
            table: "AgendaPageLinks",
            column: "PageId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AgendaPageLinks");
    }
}
