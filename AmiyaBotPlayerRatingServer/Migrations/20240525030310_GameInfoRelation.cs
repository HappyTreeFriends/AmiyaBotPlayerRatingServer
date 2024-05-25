using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiyaBotPlayerRatingServer.Migrations
{
    /// <inheritdoc />
    public partial class GameInfoRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_GameInfos_GameInfoId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_GameInfoId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "GameInfoId",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "ApplicationUserGameInfo",
                columns: table => new
                {
                    GameInfoId = table.Column<string>(type: "text", nullable: false),
                    PlayerListId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUserGameInfo", x => new { x.GameInfoId, x.PlayerListId });
                    table.ForeignKey(
                        name: "FK_ApplicationUserGameInfo_AspNetUsers_PlayerListId",
                        column: x => x.PlayerListId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationUserGameInfo_GameInfos_GameInfoId",
                        column: x => x.GameInfoId,
                        principalTable: "GameInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserGameInfo_PlayerListId",
                table: "ApplicationUserGameInfo",
                column: "PlayerListId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationUserGameInfo");

            migrationBuilder.AddColumn<string>(
                name: "GameInfoId",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_GameInfoId",
                table: "AspNetUsers",
                column: "GameInfoId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_GameInfos_GameInfoId",
                table: "AspNetUsers",
                column: "GameInfoId",
                principalTable: "GameInfos",
                principalColumn: "Id");
        }
    }
}
