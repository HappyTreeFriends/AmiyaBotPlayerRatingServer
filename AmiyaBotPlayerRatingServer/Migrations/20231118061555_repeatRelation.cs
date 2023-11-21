using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiyaBotPlayerRatingServer.Migrations
{
    /// <inheritdoc />
    public partial class repeatRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MAATasks_MAARepetitiveTasks_MAARepetitiveTaskId",
                table: "MAATasks");

            migrationBuilder.RenameColumn(
                name: "MAARepetitiveTaskId",
                table: "MAATasks",
                newName: "ParentRepetitiveTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_MAATasks_MAARepetitiveTaskId",
                table: "MAATasks",
                newName: "IX_MAATasks_ParentRepetitiveTaskId");

            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableFrom",
                table: "MAARepetitiveTasks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableTo",
                table: "MAARepetitiveTasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "MAARepetitiveTasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Parameters",
                table: "MAARepetitiveTasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "MAARepetitiveTasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_MAATasks_MAARepetitiveTasks_ParentRepetitiveTaskId",
                table: "MAATasks",
                column: "ParentRepetitiveTaskId",
                principalTable: "MAARepetitiveTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MAATasks_MAARepetitiveTasks_ParentRepetitiveTaskId",
                table: "MAATasks");

            migrationBuilder.DropColumn(
                name: "AvailableFrom",
                table: "MAARepetitiveTasks");

            migrationBuilder.DropColumn(
                name: "AvailableTo",
                table: "MAARepetitiveTasks");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "MAARepetitiveTasks");

            migrationBuilder.DropColumn(
                name: "Parameters",
                table: "MAARepetitiveTasks");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "MAARepetitiveTasks");

            migrationBuilder.RenameColumn(
                name: "ParentRepetitiveTaskId",
                table: "MAATasks",
                newName: "MAARepetitiveTaskId");

            migrationBuilder.RenameIndex(
                name: "IX_MAATasks_ParentRepetitiveTaskId",
                table: "MAATasks",
                newName: "IX_MAATasks_MAARepetitiveTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_MAATasks_MAARepetitiveTasks_MAARepetitiveTaskId",
                table: "MAATasks",
                column: "MAARepetitiveTaskId",
                principalTable: "MAARepetitiveTasks",
                principalColumn: "Id");
        }
    }
}
