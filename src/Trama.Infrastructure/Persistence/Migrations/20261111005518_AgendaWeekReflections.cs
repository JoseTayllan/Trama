using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trama.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AgendaWeekReflections : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AgendaWeekReflections",
            columns: table => new
            {
                WeekStart = table.Column<DateOnly>(type: "TEXT", nullable: false),
                WorkedWell = table.Column<string>(type: "TEXT", nullable: false),
                Adjustments = table.Column<string>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AgendaWeekReflections", x => x.WeekStart);
                table.ForeignKey(
                    name: "FK_AgendaWeekReflections_AgendaWeekRetrospectives_WeekStart",
                    column: x => x.WeekStart,
                    principalTable: "AgendaWeekRetrospectives",
                    principalColumn: "StartDate",
                    onDelete: ReferentialAction.Restrict);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AgendaWeekReflections");
    }
}
