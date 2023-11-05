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
                type: "smallint[]",
                nullable: false,
                defaultValue: new byte?[0]);

            migrationBuilder.AddColumn<byte?[]>(
                name: "ImagePayloadThumbnail",
                table: "MAAResponses",
                type: "smallint[]",
                nullable: false,
                defaultValue: new byte?[0]);
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
