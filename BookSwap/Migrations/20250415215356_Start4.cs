using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSwap.Migrations
{
    /// <inheritdoc />
    public partial class Start4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Readers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "BookOwners",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "BookOwners",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Readers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "BookOwners");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "BookOwners");
        }
    }
}
