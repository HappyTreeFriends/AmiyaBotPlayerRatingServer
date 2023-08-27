using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiyaBotPlayerRatingServer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterStatistics",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    VersionStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VersionEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SampleCount = table.Column<long>(type: "bigint", nullable: false),
                    AverageExptLevel = table.Column<int>(type: "integer", nullable: false),
                    AverageLevel = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterStatistics", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterStatistics");
        }
    }
}
