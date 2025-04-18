using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSwap.Migrations
{
    /// <inheritdoc />
    public partial class RefreshToken3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Admins_AdminName",
                table: "RefreshTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_BookOwners_BookOwnerID",
                table: "RefreshTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Readers_ReaderID",
                table: "RefreshTokens");

            migrationBuilder.RenameColumn(
                name: "ReaderID",
                table: "RefreshTokens",
                newName: "ReaderId");

            migrationBuilder.RenameColumn(
                name: "BookOwnerID",
                table: "RefreshTokens",
                newName: "BookOwnerId");

            migrationBuilder.RenameColumn(
                name: "AdminName",
                table: "RefreshTokens",
                newName: "AdminId");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_ReaderID",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_ReaderId");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_BookOwnerID",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_BookOwnerId");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_AdminName",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_AdminId");

            migrationBuilder.AlterColumn<int>(
                name: "ReaderId",
                table: "RefreshTokens",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "BookOwnerId",
                table: "RefreshTokens",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Admins_AdminId",
                table: "RefreshTokens",
                column: "AdminId",
                principalTable: "Admins",
                principalColumn: "AdminName");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_BookOwners_BookOwnerId",
                table: "RefreshTokens",
                column: "BookOwnerId",
                principalTable: "BookOwners",
                principalColumn: "BookOwnerID");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Readers_ReaderId",
                table: "RefreshTokens",
                column: "ReaderId",
                principalTable: "Readers",
                principalColumn: "ReaderID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Admins_AdminId",
                table: "RefreshTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_BookOwners_BookOwnerId",
                table: "RefreshTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Readers_ReaderId",
                table: "RefreshTokens");

            migrationBuilder.RenameColumn(
                name: "ReaderId",
                table: "RefreshTokens",
                newName: "ReaderID");

            migrationBuilder.RenameColumn(
                name: "BookOwnerId",
                table: "RefreshTokens",
                newName: "BookOwnerID");

            migrationBuilder.RenameColumn(
                name: "AdminId",
                table: "RefreshTokens",
                newName: "AdminName");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_ReaderId",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_ReaderID");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_BookOwnerId",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_BookOwnerID");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_AdminId",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_AdminName");

            migrationBuilder.AlterColumn<int>(
                name: "ReaderID",
                table: "RefreshTokens",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BookOwnerID",
                table: "RefreshTokens",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Admins_AdminName",
                table: "RefreshTokens",
                column: "AdminName",
                principalTable: "Admins",
                principalColumn: "AdminName");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_BookOwners_BookOwnerID",
                table: "RefreshTokens",
                column: "BookOwnerID",
                principalTable: "BookOwners",
                principalColumn: "BookOwnerID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Readers_ReaderID",
                table: "RefreshTokens",
                column: "ReaderID",
                principalTable: "Readers",
                principalColumn: "ReaderID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
