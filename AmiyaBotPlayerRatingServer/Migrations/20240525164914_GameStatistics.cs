using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiyaBotPlayerRatingServer.Migrations
{
    /// <inheritdoc />
    public partial class GameStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationUserMinigameStatistics",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TotalGamesPlayed = table.Column<int>(type: "integer", nullable: false),
                    TotalGamesFirstPlace = table.Column<int>(type: "integer", nullable: false),
                    TotalGamesSecondPlace = table.Column<int>(type: "integer", nullable: false),
                    TotalGamesThirdPlace = table.Column<int>(type: "integer", nullable: false),
                    TotalAnswersCorrect = table.Column<int>(type: "integer", nullable: false),
                    TotalAnswersWrong = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUserMinigameStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationUserMinigameStatistics_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserMinigameStatistics_UserId",
                table: "ApplicationUserMinigameStatistics",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationUserMinigameStatistics");
        }
    }
}
