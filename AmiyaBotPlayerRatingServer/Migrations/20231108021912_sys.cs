using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiyaBotPlayerRatingServer.Migrations
{
#pragma warning disable CS8981 // 该类型名称仅包含小写 ascii 字符。此类名称可能会成为该语言的保留值。
#pragma warning disable IDE1006 // 命名样式
    /// <inheritdoc />
    public partial class sys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystemGenerated",
                table: "MAATasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentTaskId",
                table: "MAATasks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MAATasks_ParentTaskId",
                table: "MAATasks",
                column: "ParentTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_MAATasks_MAATasks_ParentTaskId",
                table: "MAATasks",
                column: "ParentTaskId",
                principalTable: "MAATasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MAATasks_MAATasks_ParentTaskId",
                table: "MAATasks");

            migrationBuilder.DropIndex(
                name: "IX_MAATasks_ParentTaskId",
                table: "MAATasks");

            migrationBuilder.DropColumn(
                name: "IsSystemGenerated",
                table: "MAATasks");

            migrationBuilder.DropColumn(
                name: "ParentTaskId",
                table: "MAATasks");
        }
    }
}
