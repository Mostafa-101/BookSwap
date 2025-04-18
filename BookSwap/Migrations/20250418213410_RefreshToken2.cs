using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSwap.Migrations
{
    /// <inheritdoc />
    public partial class RefreshToken2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReaderID",
                table: "RefreshTokens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ReaderID",
                table: "RefreshTokens",
                column: "ReaderID");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Readers_ReaderID",
                table: "RefreshTokens",
                column: "ReaderID",
                principalTable: "Readers",
                principalColumn: "ReaderID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Readers_ReaderID",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_ReaderID",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "ReaderID",
                table: "RefreshTokens");
        }
    }
}
