using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trama.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AgendaTasks : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AgendaTasks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Title = table.Column<string>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: false),
                PlannedDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                StartTime = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                EndTime = table.Column<TimeOnly>(type: "TEXT", nullable: true),
                Category = table.Column<int>(type: "INTEGER", nullable: false),
                Status = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AgendaTasks", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AgendaTasks_PlannedDate_StartTime",
            table: "AgendaTasks",
            columns: new[] { "PlannedDate", "StartTime" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AgendaTasks");
    }
}
