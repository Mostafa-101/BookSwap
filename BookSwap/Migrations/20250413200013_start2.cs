using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSwap.Migrations
{
    /// <inheritdoc />
    public partial class start2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_BookRequests_ReaderID",
                table: "BookRequests",
                column: "ReaderID");

            migrationBuilder.AddForeignKey(
                name: "FK_BookRequests_Readers_ReaderID",
                table: "BookRequests",
                column: "ReaderID",
                principalTable: "Readers",
                principalColumn: "ReaderID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookRequests_Readers_ReaderID",
                table: "BookRequests");

            migrationBuilder.DropIndex(
                name: "IX_BookRequests_ReaderID",
                table: "BookRequests");
        }
    }
}
