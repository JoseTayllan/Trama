using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trama.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AgendaTaskStarts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AgendaTaskStarts",
            columns: table => new
            {
                TaskId = table.Column<Guid>(type: "TEXT", nullable: false),
                StartedAtUtcTicks = table.Column<long>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AgendaTaskStarts", x => x.TaskId);
                table.ForeignKey(
                    name: "FK_AgendaTaskStarts_AgendaTasks_TaskId",
                    column: x => x.TaskId,
                    principalTable: "AgendaTasks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AgendaTaskStarts");
    }
}
