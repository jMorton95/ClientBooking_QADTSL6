using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientBooking.Migrations
{
    /// <inheritdoc />
    public partial class NullableSavedById : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_SavedById",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Users_SavedById",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Users_SavedById",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Users_SavedById",
                table: "Roles");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_SavedById",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_UserUnavailabilities_Users_SavedById",
                table: "UserUnavailabilities");

            migrationBuilder.AlterColumn<int>(
                name: "SavedById",
                table: "UserUnavailabilities",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "SavedById",
                table: "Users",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "SavedById",
                table: "Settings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "SavedById",
                table: "Roles",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "SavedById",
                table: "Notifications",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "SavedById",
                table: "Clients",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "SavedById",
                table: "Bookings",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_SavedById",
                table: "Bookings",
                column: "SavedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Users_SavedById",
                table: "Clients",
                column: "SavedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Users_SavedById",
                table: "Notifications",
                column: "SavedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Users_SavedById",
                table: "Roles",
                column: "SavedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_SavedById",
                table: "Users",
                column: "SavedById",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserUnavailabilities_Users_SavedById",
                table: "UserUnavailabilities",
                column: "SavedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Users_SavedById",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Users_SavedById",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Users_SavedById",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Users_SavedById",
                table: "Roles");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_SavedById",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_UserUnavailabilities_Users_SavedById",
                table: "UserUnavailabilities");

            migrationBuilder.AlterColumn<int>(
                name: "SavedById",
                table: "UserUnavailabilities",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SavedById",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SavedById",
                table: "Settings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SavedById",
                table: "Roles",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SavedById",
                table: "Notifications",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SavedById",
                table: "Clients",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SavedById",
                table: "Bookings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Users_SavedById",
                table: "Bookings",
                column: "SavedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Users_SavedById",
                table: "Clients",
                column: "SavedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Users_SavedById",
                table: "Notifications",
                column: "SavedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Users_SavedById",
                table: "Roles",
                column: "SavedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_SavedById",
                table: "Users",
                column: "SavedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserUnavailabilities_Users_SavedById",
                table: "UserUnavailabilities",
                column: "SavedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
