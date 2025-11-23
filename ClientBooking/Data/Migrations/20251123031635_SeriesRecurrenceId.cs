using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientBooking.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeriesRecurrenceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RecurrenceSeriesId",
                table: "Bookings",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecurrenceSeriesId",
                table: "Bookings");
        }
    }
}
