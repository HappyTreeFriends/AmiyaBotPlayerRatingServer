using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiyaBotPlayerRatingServer.Migrations
{
    /// <inheritdoc />
    public partial class changed_level : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageEvolvePhase",
                table: "CharacterStatistics");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AverageEvolvePhase",
                table: "CharacterStatistics",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
