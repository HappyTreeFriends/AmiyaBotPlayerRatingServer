using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiyaBotPlayerRatingServer.Migrations
{
    /// <inheritdoc />
    public partial class TaskDate4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "ImagePayloadThumbnail",
                table: "MAAResponses",
                type: "bytea",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "smallint[]");

            migrationBuilder.AlterColumn<byte[]>(
                name: "ImagePayload",
                table: "MAAResponses",
                type: "bytea",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "smallint[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "ImagePayloadThumbnail",
                table: "MAAResponses",
                type: "smallint[]",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "ImagePayload",
                table: "MAAResponses",
                type: "smallint[]",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldNullable: true);
        }
    }
}
