using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiyaBotPlayerRatingServer.Migrations
{
    /// <inheritdoc />
    public partial class TaskDate3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte?[]>(
                name: "ImagePayload",
                table: "MAAResponses",
                type: "bytea",
                nullable: false);

            migrationBuilder.AddColumn<byte?[]>(
                name: "ImagePayloadThumbnail",
                table: "MAAResponses",
                type: "bytea",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePayload",
                table: "MAAResponses");

            migrationBuilder.DropColumn(
                name: "ImagePayloadThumbnail",
                table: "MAAResponses");
        }
    }
}
