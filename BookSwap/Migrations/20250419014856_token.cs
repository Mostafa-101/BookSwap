using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSwap.Migrations
{
    /// <inheritdoc />
    public partial class token : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Expires = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdminName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BookOwnerId = table.Column<int>(type: "int", nullable: true),
                    ReaderId = table.Column<int>(type: "int", nullable: true),
                    AdminName1 = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Admins_AdminName1",
                        column: x => x.AdminName1,
                        principalTable: "Admins",
                        principalColumn: "AdminName");
                    table.ForeignKey(
                        name: "FK_RefreshTokens_BookOwners_BookOwnerId",
                        column: x => x.BookOwnerId,
                        principalTable: "BookOwners",
                        principalColumn: "BookOwnerID");
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Readers_ReaderId",
                        column: x => x.ReaderId,
                        principalTable: "Readers",
                        principalColumn: "ReaderID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_AdminName1",
                table: "RefreshTokens",
                column: "AdminName1");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_BookOwnerId",
                table: "RefreshTokens",
                column: "BookOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ReaderId",
                table: "RefreshTokens",
                column: "ReaderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens");
        }
    }
}
