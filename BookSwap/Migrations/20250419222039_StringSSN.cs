using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookSwap.Migrations
{
    /// <inheritdoc />
    public partial class StringSSN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ssn",
                table: "BookOwners");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "BookOwners",
                newName: "EncryptedSsn");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "BookOwners",
                newName: "EncryptedPhoneNumber");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedEmail",
                table: "BookOwners",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedEmail",
                table: "BookOwners");

            migrationBuilder.RenameColumn(
                name: "EncryptedSsn",
                table: "BookOwners",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "EncryptedPhoneNumber",
                table: "BookOwners",
                newName: "Email");

            migrationBuilder.AddColumn<int>(
                name: "ssn",
                table: "BookOwners",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
