using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiyaBotPlayerRatingServer.Migrations
{
    /// <inheritdoc />
    public partial class MAAEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MAAConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    DeviceIdentity = table.Column<string>(type: "text", nullable: true),
                    UserIdentity = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MAAConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MAAConnections_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MAATasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MAATasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MAATasks_MAAConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "MAAConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MAAResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MAAResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MAAResponses_MAATasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "MAATasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MAAConnections_UserId",
                table: "MAAConnections",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MAAResponses_TaskId",
                table: "MAAResponses",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_MAATasks_ConnectionId",
                table: "MAATasks",
                column: "ConnectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MAAResponses");

            migrationBuilder.DropTable(
                name: "MAATasks");

            migrationBuilder.DropTable(
                name: "MAAConnections");
        }
    }
}
