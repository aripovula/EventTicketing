using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EventTicketing.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEventTypeAndTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Events",
                newName: "StartTime");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "Events",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "Events",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AvailableSeats", "Description", "EndTime", "EventType", "StartTime", "TotalSeats" },
                values: new object[] { 48, "An intimate evening of live jazz featuring the city's finest musicians. Expect smooth bebop, cool jazz, and soulful improvisation.", new DateTime(2026, 5, 2, 22, 0, 0, 0, DateTimeKind.Unspecified), "Music", new DateTime(2026, 5, 2, 19, 0, 0, 0, DateTimeKind.Unspecified), 120 });

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AvailableSeats", "Description", "EndTime", "EventType", "StartTime" },
                values: new object[] { 212, "A full-day conference on modern software development — AI, distributed systems, DevOps and beyond. Keynotes from industry leaders.", new DateTime(2026, 5, 5, 18, 0, 0, 0, DateTimeKind.Unspecified), "Tech", new DateTime(2026, 5, 5, 9, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.InsertData(
                table: "Events",
                columns: new[] { "Id", "AvailableSeats", "Description", "EndTime", "EventType", "ImageUrl", "Price", "StartTime", "Title", "TotalSeats", "Venue" },
                values: new object[,]
                {
                    { 3, 134, "Stand-up comedy night featuring five rising comedians. Uncensored, hilarious, and perfect for a night out.", new DateTime(2026, 5, 7, 22, 30, 0, 0, DateTimeKind.Unspecified), "Comedy", "https://images.unsplash.com/photo-1527224857830-43a7acc85260?w=800&q=80", 18.00m, new DateTime(2026, 5, 7, 20, 0, 0, 0, DateTimeKind.Unspecified), "Comedy Showcase", 200, "Laugh Factory" },
                    { 4, 870, "Conference finals — top two city teams battle for the championship. High energy, packed arena, one winner.", new DateTime(2026, 5, 9, 21, 30, 0, 0, DateTimeKind.Unspecified), "Sports", "https://images.unsplash.com/photo-1546519638-68e109498ffc?w=800&q=80", 45.00m, new DateTime(2026, 5, 9, 19, 0, 0, 0, DateTimeKind.Unspecified), "Championship Basketball", 3000, "City Arena" },
                    { 5, 91, "A full-day gathering for founders, investors, and innovators. Panels on fundraising, product-market fit, and scaling.", new DateTime(2026, 5, 12, 18, 0, 0, 0, DateTimeKind.Unspecified), "Business", "https://images.unsplash.com/photo-1515187029135-18ee286d815b?w=800&q=80", 199.00m, new DateTime(2026, 5, 12, 9, 0, 0, 0, DateTimeKind.Unspecified), "Startup Summit", 300, "Grand Ballroom" },
                    { 6, 155, "A spectacular evening of Broadway's greatest hits performed by a cast of West End and Broadway veterans.", new DateTime(2026, 5, 14, 22, 0, 0, 0, DateTimeKind.Unspecified), "Theater", "https://images.unsplash.com/photo-1507676184212-d03ab07a01bf?w=800&q=80", 85.00m, new DateTime(2026, 5, 14, 19, 30, 0, 0, DateTimeKind.Unspecified), "Broadway Hits Gala", 400, "Empire Theatre" },
                    { 7, 623, "Over 50 local restaurants and wineries in one place. Tastings, live cooking demos, and expert-led wine pairings.", new DateTime(2026, 5, 16, 20, 0, 0, 0, DateTimeKind.Unspecified), "Food", "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=800&q=80", 35.00m, new DateTime(2026, 5, 16, 12, 0, 0, 0, DateTimeKind.Unspecified), "Food & Wine Festival", 1000, "Riverside Park" },
                    { 8, 388, "A curated showcase of contemporary works from 30 emerging artists. Sculpture, digital art, painting, and installation.", new DateTime(2026, 5, 17, 18, 0, 0, 0, DateTimeKind.Unspecified), "Art", "https://images.unsplash.com/photo-1579783900882-c0d3dad7b119?w=800&q=80", 15.00m, new DateTime(2026, 5, 17, 10, 0, 0, 0, DateTimeKind.Unspecified), "Modern Art Exhibition", 500, "City Gallery" },
                    { 9, 2140, "An explosive night of classic rock anthems. Three tribute bands covering Led Zeppelin, Queen, and AC/DC back to back.", new DateTime(2026, 5, 20, 23, 0, 0, 0, DateTimeKind.Unspecified), "Music", "https://images.unsplash.com/photo-1470229722913-7c0e2dbbafd3?w=800&q=80", 65.00m, new DateTime(2026, 5, 20, 19, 0, 0, 0, DateTimeKind.Unspecified), "Rock Legends Live", 5000, "Civic Stadium" },
                    { 10, 1047, "Annual 42 km city marathon through iconic landmarks. Open to all levels with 5 km and 10 km fun-run categories.", new DateTime(2026, 5, 21, 13, 0, 0, 0, DateTimeKind.Unspecified), "Sports", "https://images.unsplash.com/photo-1552674605-db6ffd4facb5?w=800&q=80", 30.00m, new DateTime(2026, 5, 21, 7, 0, 0, 0, DateTimeKind.Unspecified), "Marathon City Run", 2000, "City Park" },
                    { 11, 22, "Journey through Italy's finest wine regions. Six guided tastings paired with artisan charcuterie and cheese.", new DateTime(2026, 5, 24, 21, 0, 0, 0, DateTimeKind.Unspecified), "Food", "https://images.unsplash.com/photo-1414235077428-338989a2e8c0?w=800&q=80", 55.00m, new DateTime(2026, 5, 24, 18, 0, 0, 0, DateTimeKind.Unspecified), "Italian Wine Tasting", 80, "Vineyard Lounge" },
                    { 12, 9, "Hands-on evening workshop covering lighting, composition, and post-processing. Bring your camera or use a loaner.", new DateTime(2026, 5, 24, 20, 0, 0, 0, DateTimeKind.Unspecified), "Art", "https://images.unsplash.com/photo-1452780212461-64df12a92440?w=800&q=80", 75.00m, new DateTime(2026, 5, 24, 17, 0, 0, 0, DateTimeKind.Unspecified), "Photography Workshop", 30, "Art Studio" },
                    { 13, 204, "The City Philharmonic performs Beethoven's 5th and Dvořák's New World Symphony in a grand evening concert.", new DateTime(2026, 5, 28, 22, 0, 0, 0, DateTimeKind.Unspecified), "Music", "https://images.unsplash.com/photo-1514320291840-2e0a9bf2a9ae?w=800&q=80", 70.00m, new DateTime(2026, 5, 28, 19, 30, 0, 0, DateTimeKind.Unspecified), "Symphony Orchestra", 600, "Concert Hall" },
                    { 14, 67, "The biggest comedy event of the month. Seven headliners, two hours of non-stop laughs, free drinks on arrival.", new DateTime(2026, 5, 28, 22, 30, 0, 0, DateTimeKind.Unspecified), "Comedy", "https://images.unsplash.com/photo-1527224857830-43a7acc85260?w=800&q=80", 22.00m, new DateTime(2026, 5, 28, 20, 0, 0, 0, DateTimeKind.Unspecified), "City Comedy Night", 200, "Laugh House" },
                    { 15, 173, "A prestigious evening of classical and contemporary ballet performed by the National Dance Company.", new DateTime(2026, 5, 30, 21, 30, 0, 0, DateTimeKind.Unspecified), "Theater", "https://images.unsplash.com/photo-1598899134739-24c46f58b8c0?w=800&q=80", 90.00m, new DateTime(2026, 5, 30, 19, 0, 0, 0, DateTimeKind.Unspecified), "Ballet Gala", 350, "City Theater" },
                    { 16, 1532, "70+ street food vendors from 30 countries. Flavours, music, and fun for the whole family.", new DateTime(2026, 6, 1, 21, 0, 0, 0, DateTimeKind.Unspecified), "Food", "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=800&q=80", 10.00m, new DateTime(2026, 6, 1, 11, 0, 0, 0, DateTimeKind.Unspecified), "Street Food Carnival", 2000, "Central Square" },
                    { 17, 789, "City-wide tennis open tournament. Singles and doubles brackets for all skill levels. Spectator entry included.", new DateTime(2026, 6, 3, 18, 0, 0, 0, DateTimeKind.Unspecified), "Sports", "https://images.unsplash.com/photo-1461896836934-ffe607ba8211?w=800&q=80", 40.00m, new DateTime(2026, 6, 3, 10, 0, 0, 0, DateTimeKind.Unspecified), "Tennis Open", 1500, "Sports Complex" },
                    { 18, 118, "Festival-circuit award-winning indie films screened back to back with Q&A sessions with the directors.", new DateTime(2026, 6, 5, 23, 0, 0, 0, DateTimeKind.Unspecified), "Art", "https://images.unsplash.com/photo-1536924430914-91f9e2041b83?w=800&q=80", 22.00m, new DateTime(2026, 6, 5, 19, 0, 0, 0, DateTimeKind.Unspecified), "Indie Film Screening", 200, "Cinema House" },
                    { 19, 14, "Lazy Sunday brunch with live jazz quartet. Three-course brunch menu with bottomless mimosas included.", new DateTime(2026, 6, 7, 14, 0, 0, 0, DateTimeKind.Unspecified), "Music", "https://images.unsplash.com/photo-1548163111-bc419d75fef4?w=800&q=80", 45.00m, new DateTime(2026, 6, 7, 11, 0, 0, 0, DateTimeKind.Unspecified), "Jazz Brunch", 60, "Rooftop Lounge" },
                    { 20, 11, "Intensive one-day bootcamp. Build and deploy a full-stack web app from scratch. Beginners welcome.", new DateTime(2026, 6, 8, 17, 0, 0, 0, DateTimeKind.Unspecified), "Tech", "https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=800&q=80", 299.00m, new DateTime(2026, 6, 8, 9, 0, 0, 0, DateTimeKind.Unspecified), "Web Dev Bootcamp", 40, "Tech Hub" },
                    { 21, 201, "Four hours of non-stop stand-up comedy. 12 comedians, rotating every 20 minutes. Your cheeks will hurt.", new DateTime(2026, 6, 12, 23, 0, 0, 0, DateTimeKind.Unspecified), "Comedy", "https://images.unsplash.com/photo-1527224857830-43a7acc85260?w=800&q=80", 28.00m, new DateTime(2026, 6, 12, 19, 0, 0, 0, DateTimeKind.Unspecified), "Comedy Marathon", 300, "Comedy Palace" },
                    { 22, 63, "Latin beats, professional instructors, and a packed dance floor. Beginners' crash course at 7pm before the social.", new DateTime(2026, 6, 13, 23, 0, 0, 0, DateTimeKind.Unspecified), "Music", "https://images.unsplash.com/photo-1504609813442-a8924e83f76e?w=800&q=80", 30.00m, new DateTime(2026, 6, 13, 20, 0, 0, 0, DateTimeKind.Unspecified), "Salsa Dance Night", 150, "Latin Club" },
                    { 23, 88, "Deep dives into DeFi, Web3 infrastructure, and enterprise blockchain. Networking dinner included.", new DateTime(2026, 6, 14, 18, 0, 0, 0, DateTimeKind.Unspecified), "Business", "https://images.unsplash.com/photo-1542744173-8e7e53415bb0?w=800&q=80", 249.00m, new DateTime(2026, 6, 14, 9, 0, 0, 0, DateTimeKind.Unspecified), "Blockchain Summit", 200, "Conference Center" },
                    { 24, 144, "Classic films under the stars. Bring a blanket, grab a cocktail from the bar, and enjoy cinema the way it was meant to be.", new DateTime(2026, 6, 15, 23, 0, 0, 0, DateTimeKind.Unspecified), "Art", "https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=800&q=80", 18.00m, new DateTime(2026, 6, 15, 20, 0, 0, 0, DateTimeKind.Unspecified), "Outdoor Cinema Night", 200, "Rooftop Garden" },
                    { 25, 322, "Pop-up art market showcasing 40 local artists. Original paintings, prints, ceramics, and live art demonstrations.", new DateTime(2026, 6, 15, 22, 0, 0, 0, DateTimeKind.Unspecified), "Art", "https://images.unsplash.com/photo-1579783900882-c0d3dad7b119?w=800&q=80", 5.00m, new DateTime(2026, 6, 15, 18, 0, 0, 0, DateTimeKind.Unspecified), "Evening Art Market", 500, "Warehouse District" },
                    { 26, 3211, "The biggest outdoor music festival of the summer. Eight bands across two stages, food trucks, and fireworks finale.", new DateTime(2026, 6, 19, 23, 0, 0, 0, DateTimeKind.Unspecified), "Music", "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?w=800&q=80", 85.00m, new DateTime(2026, 6, 19, 16, 0, 0, 0, DateTimeKind.Unspecified), "Summer Rock Fest", 8000, "Lakeside Amphitheatre" },
                    { 27, 2455, "30 food trucks, craft beers, and live acoustic music. Vote for your favourite truck and win prizes.", new DateTime(2026, 6, 21, 20, 0, 0, 0, DateTimeKind.Unspecified), "Food", "https://images.unsplash.com/photo-1565123409695-7b5ef63a2efb?w=800&q=80", 8.00m, new DateTime(2026, 6, 21, 12, 0, 0, 0, DateTimeKind.Unspecified), "Food Truck Rally", 3000, "Waterfront Plaza" },
                    { 28, 198, "A modern retelling of Shakespeare's Hamlet set in a near-future dystopia. Critically acclaimed production.", new DateTime(2026, 6, 26, 21, 30, 0, 0, DateTimeKind.Unspecified), "Theater", "https://images.unsplash.com/photo-1507676184212-d03ab07a01bf?w=800&q=80", 65.00m, new DateTime(2026, 6, 26, 19, 0, 0, 0, DateTimeKind.Unspecified), "Theater Night: Hamlet", 300, "Civic Theatre" },
                    { 29, 567, "Al fresco jazz concert in the park. Bring a picnic, bring friends, and let the music carry the evening.", new DateTime(2026, 6, 28, 21, 0, 0, 0, DateTimeKind.Unspecified), "Music", "https://images.unsplash.com/photo-1548163111-bc419d75fef4?w=800&q=80", 35.00m, new DateTime(2026, 6, 28, 18, 0, 0, 0, DateTimeKind.Unspecified), "Summer Jazz Concert", 1000, "Park Amphitheatre" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "Events",
                newName: "Date");

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "AvailableSeats", "Date", "Description", "TotalSeats" },
                values: new object[] { 100, new DateTime(2026, 8, 15, 20, 0, 0, 0, DateTimeKind.Unspecified), "An evening of live jazz music.", 100 });

            migrationBuilder.UpdateData(
                table: "Events",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "AvailableSeats", "Date", "Description" },
                values: new object[] { 500, new DateTime(2026, 7, 10, 9, 0, 0, 0, DateTimeKind.Unspecified), "A full-day conference on modern software development." });
        }
    }
}
