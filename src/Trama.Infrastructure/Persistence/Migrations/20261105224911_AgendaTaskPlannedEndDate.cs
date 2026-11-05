using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trama.Infrastructure.Persistence.Migrations;
/// <inheritdoc />
public partial class AgendaTaskPlannedEndDate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateOnly>(
            name: "PlannedEndDate",
            table: "AgendaTasks",
            type: "TEXT",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PlannedEndDate",
            table: "AgendaTasks");
    }
}
