using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientBooking.Migrations
{
    /// <inheritdoc />
    public partial class SettingsNoCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Settings_Users_SavedById",
                table: "Settings");

            migrationBuilder.AddForeignKey(
                name: "FK_Settings_Users_SavedById",
                table: "Settings",
                column: "SavedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Settings_Users_SavedById",
                table: "Settings");

            migrationBuilder.AddForeignKey(
                name: "FK_Settings_Users_SavedById",
                table: "Settings",
                column: "SavedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
