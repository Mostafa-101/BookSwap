using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSwap.Migrations
{
    /// <inheritdoc />
    public partial class Start5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PostDate",
                table: "BookPosts",
                newName: "PublicationDate");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "BookPosts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Language",
                table: "BookPosts");

            migrationBuilder.RenameColumn(
                name: "PublicationDate",
                table: "BookPosts",
                newName: "PostDate");
        }
    }
}
