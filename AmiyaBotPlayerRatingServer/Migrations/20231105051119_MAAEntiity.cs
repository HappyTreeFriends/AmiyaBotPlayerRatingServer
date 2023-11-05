using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiyaBotPlayerRatingServer.Migrations
{
    /// <inheritdoc />
    public partial class MAAEntiity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MAAResponses_MAATasks_TaskId1",
                table: "MAAResponses");

            migrationBuilder.DropForeignKey(
                name: "FK_MAATasks_MAAConnections_ConnectionId1",
                table: "MAATasks");

            migrationBuilder.DropIndex(
                name: "IX_MAATasks_ConnectionId1",
                table: "MAATasks");

            migrationBuilder.DropIndex(
                name: "IX_MAAResponses_TaskId1",
                table: "MAAResponses");

            migrationBuilder.DropColumn(
                name: "ConnectionId1",
                table: "MAATasks");

            migrationBuilder.DropColumn(
                name: "TaskId1",
                table: "MAAResponses");

            migrationBuilder.AlterColumn<Guid>(
                name: "ConnectionId",
                table: "MAATasks",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<Guid>(
                name: "TaskId",
                table: "MAAResponses",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_MAATasks_ConnectionId",
                table: "MAATasks",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_MAAResponses_TaskId",
                table: "MAAResponses",
                column: "TaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_MAAResponses_MAATasks_TaskId",
                table: "MAAResponses",
                column: "TaskId",
                principalTable: "MAATasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MAATasks_MAAConnections_ConnectionId",
                table: "MAATasks",
                column: "ConnectionId",
                principalTable: "MAAConnections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MAAResponses_MAATasks_TaskId",
                table: "MAAResponses");

            migrationBuilder.DropForeignKey(
                name: "FK_MAATasks_MAAConnections_ConnectionId",
                table: "MAATasks");

            migrationBuilder.DropIndex(
                name: "IX_MAATasks_ConnectionId",
                table: "MAATasks");

            migrationBuilder.DropIndex(
                name: "IX_MAAResponses_TaskId",
                table: "MAAResponses");

            migrationBuilder.AlterColumn<string>(
                name: "ConnectionId",
                table: "MAATasks",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ConnectionId1",
                table: "MAATasks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "TaskId",
                table: "MAAResponses",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "TaskId1",
                table: "MAAResponses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_MAATasks_ConnectionId1",
                table: "MAATasks",
                column: "ConnectionId1");

            migrationBuilder.CreateIndex(
                name: "IX_MAAResponses_TaskId1",
                table: "MAAResponses",
                column: "TaskId1");

            migrationBuilder.AddForeignKey(
                name: "FK_MAAResponses_MAATasks_TaskId1",
                table: "MAAResponses",
                column: "TaskId1",
                principalTable: "MAATasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MAATasks_MAAConnections_ConnectionId1",
                table: "MAATasks",
                column: "ConnectionId1",
                principalTable: "MAAConnections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
