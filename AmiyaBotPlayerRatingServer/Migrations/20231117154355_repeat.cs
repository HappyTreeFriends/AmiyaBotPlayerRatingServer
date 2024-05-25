using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiyaBotPlayerRatingServer.Migrations
{
#pragma warning disable CS8981 // 该类型名称仅包含小写 ascii 字符。此类名称可能会成为该语言的保留值。
#pragma warning disable IDE1006 // 命名样式
    /// <inheritdoc />
    public partial class repeat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableAt",
                table: "MAATasks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "MAARepetitiveTaskId",
                table: "MAATasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MAARepetitiveTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConnectionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UtcCronString = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MAARepetitiveTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MAARepetitiveTasks_MAAConnections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "MAAConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MAATasks_MAARepetitiveTaskId",
                table: "MAATasks",
                column: "MAARepetitiveTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_MAARepetitiveTasks_ConnectionId",
                table: "MAARepetitiveTasks",
                column: "ConnectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_MAATasks_MAARepetitiveTasks_MAARepetitiveTaskId",
                table: "MAATasks",
                column: "MAARepetitiveTaskId",
                principalTable: "MAARepetitiveTasks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MAATasks_MAARepetitiveTasks_MAARepetitiveTaskId",
                table: "MAATasks");

            migrationBuilder.DropTable(
                name: "MAARepetitiveTasks");

            migrationBuilder.DropIndex(
                name: "IX_MAATasks_MAARepetitiveTaskId",
                table: "MAATasks");

            migrationBuilder.DropColumn(
                name: "AvailableAt",
                table: "MAATasks");

            migrationBuilder.DropColumn(
                name: "MAARepetitiveTaskId",
                table: "MAATasks");
        }
    }
}
