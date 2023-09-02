using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiyaBotPlayerRatingServer.Migrations
{
    /// <inheritdoc />
    public partial class add_detail_property : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageExptLevel",
                table: "CharacterStatistics");

            migrationBuilder.AlterColumn<double>(
                name: "AverageLevel",
                table: "CharacterStatistics",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "AverageEquipLevel",
                table: "CharacterStatistics",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "AverageEvolvePhase",
                table: "CharacterStatistics",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AverageSkillLevel",
                table: "CharacterStatistics",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "AverageSpecializeLevel",
                table: "CharacterStatistics",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CharacterId",
                table: "CharacterStatistics",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageEquipLevel",
                table: "CharacterStatistics");

            migrationBuilder.DropColumn(
                name: "AverageEvolvePhase",
                table: "CharacterStatistics");

            migrationBuilder.DropColumn(
                name: "AverageSkillLevel",
                table: "CharacterStatistics");

            migrationBuilder.DropColumn(
                name: "AverageSpecializeLevel",
                table: "CharacterStatistics");

            migrationBuilder.DropColumn(
                name: "CharacterId",
                table: "CharacterStatistics");

            migrationBuilder.AlterColumn<int>(
                name: "AverageLevel",
                table: "CharacterStatistics",
                type: "integer",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AddColumn<int>(
                name: "AverageExptLevel",
                table: "CharacterStatistics",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
