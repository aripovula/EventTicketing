using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventTicketing.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBluesSoulEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Events",
                columns: new[] { "Id", "AvailableSeats", "Description", "EndTime", "EventType", "ImageUrl", "Price", "StartTime", "Title", "TotalSeats", "Venue" },
                values: new object[] { 30, 92, "An electric night of blues and soul with back-to-back sets from four acclaimed artists. Late bar, warm vibes, unforgettable music.", new DateTime(2026, 5, 2, 23, 0, 0, 0, DateTimeKind.Unspecified), "Music", "https://images.unsplash.com/photo-1516280440614-37939bbacd81?w=800&q=80", 20.00m, new DateTime(2026, 5, 2, 20, 30, 0, 0, DateTimeKind.Unspecified), "Blues & Soul Evening", 150, "The Rusty String" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 30);
        }
    }
}
