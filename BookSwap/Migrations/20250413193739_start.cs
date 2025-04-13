using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSwap.Migrations
{
    /// <inheritdoc />
    public partial class start : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    AdminName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.AdminName);
                });

            migrationBuilder.CreateTable(
                name: "BookOwners",
                columns: table => new
                {
                    BookOwnerID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookOwnerName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ssn = table.Column<int>(type: "int", nullable: false),
                    RequestStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookOwners", x => x.BookOwnerID);
                });

            migrationBuilder.CreateTable(
                name: "Readers",
                columns: table => new
                {
                    ReaderID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReaderName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Readers", x => x.ReaderID);
                });

            migrationBuilder.CreateTable(
                name: "BookPosts",
                columns: table => new
                {
                    BookPostID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookOwnerID = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Genre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ISBN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PostDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Price = table.Column<int>(type: "int", nullable: false),
                    CoverPhoto = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    PostStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookPosts", x => x.BookPostID);
                    table.ForeignKey(
                        name: "FK_BookPosts_BookOwners_BookOwnerID",
                        column: x => x.BookOwnerID,
                        principalTable: "BookOwners",
                        principalColumn: "BookOwnerID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookRequests",
                columns: table => new
                {
                    RequsetID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookPostID = table.Column<int>(type: "int", nullable: false),
                    ReaderID = table.Column<int>(type: "int", nullable: false),
                    RequsetStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookRequests", x => x.RequsetID);
                    table.ForeignKey(
                        name: "FK_BookRequests_BookPosts_BookPostID",
                        column: x => x.BookPostID,
                        principalTable: "BookPosts",
                        principalColumn: "BookPostID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    CommentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReaderID = table.Column<int>(type: "int", nullable: false),
                    BookPostID = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.CommentID);
                    table.ForeignKey(
                        name: "FK_Comments_BookPosts_BookPostID",
                        column: x => x.BookPostID,
                        principalTable: "BookPosts",
                        principalColumn: "BookPostID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Readers_ReaderID",
                        column: x => x.ReaderID,
                        principalTable: "Readers",
                        principalColumn: "ReaderID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Likes",
                columns: table => new
                {
                    LikeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReaderID = table.Column<int>(type: "int", nullable: false),
                    BookPostID = table.Column<int>(type: "int", nullable: false),
                    IsLike = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Likes", x => x.LikeID);
                    table.ForeignKey(
                        name: "FK_Likes_BookPosts_BookPostID",
                        column: x => x.BookPostID,
                        principalTable: "BookPosts",
                        principalColumn: "BookPostID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Likes_Readers_ReaderID",
                        column: x => x.ReaderID,
                        principalTable: "Readers",
                        principalColumn: "ReaderID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Replies",
                columns: table => new
                {
                    ReplyID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommentID = table.Column<int>(type: "int", nullable: false),
                    ReaderID = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Replies", x => x.ReplyID);
                    table.ForeignKey(
                        name: "FK_Replies_Comments_CommentID",
                        column: x => x.CommentID,
                        principalTable: "Comments",
                        principalColumn: "CommentID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Replies_Readers_ReaderID",
                        column: x => x.ReaderID,
                        principalTable: "Readers",
                        principalColumn: "ReaderID",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookPosts_BookOwnerID",
                table: "BookPosts",
                column: "BookOwnerID");

            migrationBuilder.CreateIndex(
                name: "IX_BookRequests_BookPostID",
                table: "BookRequests",
                column: "BookPostID");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_BookPostID",
                table: "Comments",
                column: "BookPostID");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ReaderID",
                table: "Comments",
                column: "ReaderID");

            migrationBuilder.CreateIndex(
                name: "IX_Likes_BookPostID",
                table: "Likes",
                column: "BookPostID");

            migrationBuilder.CreateIndex(
                name: "IX_Likes_ReaderID",
                table: "Likes",
                column: "ReaderID");

            migrationBuilder.CreateIndex(
                name: "IX_Replies_CommentID",
                table: "Replies",
                column: "CommentID");

            migrationBuilder.CreateIndex(
                name: "IX_Replies_ReaderID",
                table: "Replies",
                column: "ReaderID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "BookRequests");

            migrationBuilder.DropTable(
                name: "Likes");

            migrationBuilder.DropTable(
                name: "Replies");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "BookPosts");

            migrationBuilder.DropTable(
                name: "Readers");

            migrationBuilder.DropTable(
                name: "BookOwners");
        }
    }
}
