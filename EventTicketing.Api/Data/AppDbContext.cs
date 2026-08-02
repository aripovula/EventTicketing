using EventTicketing.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Card> Cards => Set<Card>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // When a user is deleted, null out their orders rather than blocking
        // the delete or cascade-deleting order history.
        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.Email);

        modelBuilder.Entity<Event>().HasData(
            // ── May 2026 ──────────────────────────────────────────────────────────
            new Event
            {
                Id = 1,
                Title = "Jazz Night",
                Description = "An intimate evening of live jazz featuring the city's finest musicians. Expect smooth bebop, cool jazz, and soulful improvisation.",
                StartTime = new DateTime(2026, 5, 2, 19, 0, 0),
                EndTime = new DateTime(2026, 5, 2, 22, 0, 0),
                Venue = "Blue Note Club",
                TotalSeats = 120,
                AvailableSeats = 48,
                Price = 25.00m,
                EventType = "Music",
                ImageUrl = "https://images.unsplash.com/photo-1548163111-bc419d75fef4?w=800&q=80",
            },
            new Event
            {
                Id = 2,
                Title = "Tech Conference 2026",
                Description = "A full-day conference on modern software development — AI, distributed systems, DevOps and beyond. Keynotes from industry leaders.",
                StartTime = new DateTime(2026, 5, 5, 9, 0, 0),
                EndTime = new DateTime(2026, 5, 5, 18, 0, 0),
                Venue = "City Convention Centre",
                TotalSeats = 500,
                AvailableSeats = 212,
                Price = 149.00m,
                EventType = "Tech",
                ImageUrl = "https://images.unsplash.com/photo-1582192730841-2a682d7375f9?w=800&q=80",
            },
            new Event
            {
                Id = 3,
                Title = "Comedy Showcase",
                Description = "Stand-up comedy night featuring five rising comedians. Uncensored, hilarious, and perfect for a night out.",
                StartTime = new DateTime(2026, 5, 7, 20, 0, 0),
                EndTime = new DateTime(2026, 5, 7, 22, 30, 0),
                Venue = "Laugh Factory",
                TotalSeats = 200,
                AvailableSeats = 134,
                Price = 18.00m,
                EventType = "Comedy",
                ImageUrl = "https://images.unsplash.com/photo-1527224857830-43a7acc85260?w=800&q=80",
            },
            new Event
            {
                Id = 4,
                Title = "Championship Basketball",
                Description = "Conference finals — top two city teams battle for the championship. High energy, packed arena, one winner.",
                StartTime = new DateTime(2026, 5, 9, 19, 0, 0),
                EndTime = new DateTime(2026, 5, 9, 21, 30, 0),
                Venue = "City Arena",
                TotalSeats = 3000,
                AvailableSeats = 870,
                Price = 45.00m,
                EventType = "Sports",
                ImageUrl = "https://images.unsplash.com/photo-1546519638-68e109498ffc?w=800&q=80",
            },
            new Event
            {
                Id = 5,
                Title = "Startup Summit",
                Description = "A full-day gathering for founders, investors, and innovators. Panels on fundraising, product-market fit, and scaling.",
                StartTime = new DateTime(2026, 5, 12, 9, 0, 0),
                EndTime = new DateTime(2026, 5, 12, 18, 0, 0),
                Venue = "Grand Ballroom",
                TotalSeats = 300,
                AvailableSeats = 91,
                Price = 199.00m,
                EventType = "Business",
                ImageUrl = "https://images.unsplash.com/photo-1515187029135-18ee286d815b?w=800&q=80",
            },
            new Event
            {
                Id = 6,
                Title = "Broadway Hits Gala",
                Description = "A spectacular evening of Broadway's greatest hits performed by a cast of West End and Broadway veterans.",
                StartTime = new DateTime(2026, 5, 14, 19, 30, 0),
                EndTime = new DateTime(2026, 5, 14, 22, 0, 0),
                Venue = "Empire Theatre",
                TotalSeats = 400,
                AvailableSeats = 155,
                Price = 85.00m,
                EventType = "Theater",
                ImageUrl = "https://images.unsplash.com/photo-1507676184212-d03ab07a01bf?w=800&q=80",
            },
            new Event
            {
                Id = 7,
                Title = "Food & Wine Festival",
                Description = "Over 50 local restaurants and wineries in one place. Tastings, live cooking demos, and expert-led wine pairings.",
                StartTime = new DateTime(2026, 5, 16, 12, 0, 0),
                EndTime = new DateTime(2026, 5, 16, 20, 0, 0),
                Venue = "Riverside Park",
                TotalSeats = 1000,
                AvailableSeats = 623,
                Price = 35.00m,
                EventType = "Food",
                ImageUrl = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?w=800&q=80",
            },
            new Event
            {
                Id = 8,
                Title = "Modern Art Exhibition",
                Description = "A curated showcase of contemporary works from 30 emerging artists. Sculpture, digital art, painting, and installation.",
                StartTime = new DateTime(2026, 5, 17, 10, 0, 0),
                EndTime = new DateTime(2026, 5, 17, 18, 0, 0),
                Venue = "City Gallery",
                TotalSeats = 500,
                AvailableSeats = 388,
                Price = 15.00m,
                EventType = "Art",
                ImageUrl = "https://images.unsplash.com/photo-1579783900882-c0d3dad7b119?w=800&q=80",
            },
            new Event
            {
                Id = 9,
                Title = "Rock Legends Live",
                Description = "An explosive night of classic rock anthems. Three tribute bands covering Led Zeppelin, Queen, and AC/DC back to back.",
                StartTime = new DateTime(2026, 5, 20, 19, 0, 0),
                EndTime = new DateTime(2026, 5, 20, 23, 0, 0),
                Venue = "Civic Stadium",
                TotalSeats = 5000,
                AvailableSeats = 2140,
                Price = 65.00m,
                EventType = "Music",
                ImageUrl = "https://images.unsplash.com/photo-1470229722913-7c0e2dbbafd3?w=800&q=80",
            },
            new Event
            {
                Id = 10,
                Title = "Marathon City Run",
                Description = "Annual 42 km city marathon through iconic landmarks. Open to all levels with 5 km and 10 km fun-run categories.",
                StartTime = new DateTime(2026, 5, 21, 7, 0, 0),
                EndTime = new DateTime(2026, 5, 21, 13, 0, 0),
                Venue = "City Park",
                TotalSeats = 2000,
                AvailableSeats = 1047,
                Price = 30.00m,
                EventType = "Sports",
                ImageUrl = "https://images.unsplash.com/photo-1552674605-db6ffd4facb5?w=800&q=80",
            },
            // ── Conflict pair A: Italian Wine Tasting vs Photography Workshop (May 24) ──
            new Event
            {
                Id = 11,
                Title = "Italian Wine Tasting",
                Description = "Journey through Italy's finest wine regions. Six guided tastings paired with artisan charcuterie and cheese.",
                StartTime = new DateTime(2026, 5, 24, 18, 0, 0),
                EndTime = new DateTime(2026, 5, 24, 21, 0, 0),
                Venue = "Vineyard Lounge",
                TotalSeats = 80,
                AvailableSeats = 22,
                Price = 55.00m,
                EventType = "Food",
                ImageUrl = "https://images.unsplash.com/photo-1414235077428-338989a2e8c0?w=800&q=80",
            },
            new Event
            {
                Id = 12,
                Title = "Photography Workshop",
                Description = "Hands-on evening workshop covering lighting, composition, and post-processing. Bring your camera or use a loaner.",
                StartTime = new DateTime(2026, 5, 24, 17, 0, 0),
                EndTime = new DateTime(2026, 5, 24, 20, 0, 0),
                Venue = "Art Studio",
                TotalSeats = 30,
                AvailableSeats = 9,
                Price = 75.00m,
                EventType = "Art",
                ImageUrl = "https://images.unsplash.com/photo-1452780212461-64df12a92440?w=800&q=80",
            },
            // ── Conflict pair B: Symphony Orchestra vs City Comedy Night (May 28) ──
            new Event
            {
                Id = 13,
                Title = "Symphony Orchestra",
                Description = "The City Philharmonic performs Beethoven's 5th and Dvořák's New World Symphony in a grand evening concert.",
                StartTime = new DateTime(2026, 5, 28, 19, 30, 0),
                EndTime = new DateTime(2026, 5, 28, 22, 0, 0),
                Venue = "Concert Hall",
                TotalSeats = 600,
                AvailableSeats = 204,
                Price = 70.00m,
                EventType = "Music",
                ImageUrl = "https://images.unsplash.com/photo-1514320291840-2e0a9bf2a9ae?w=800&q=80",
            },
            new Event
            {
                Id = 14,
                Title = "City Comedy Night",
                Description = "The biggest comedy event of the month. Seven headliners, two hours of non-stop laughs, free drinks on arrival.",
                StartTime = new DateTime(2026, 5, 28, 20, 0, 0),
                EndTime = new DateTime(2026, 5, 28, 22, 30, 0),
                Venue = "Laugh House",
                TotalSeats = 200,
                AvailableSeats = 67,
                Price = 22.00m,
                EventType = "Comedy",
                ImageUrl = "https://images.unsplash.com/photo-1527224857830-43a7acc85260?w=800&q=80",
            },
            new Event
            {
                Id = 15,
                Title = "Ballet Gala",
                Description = "A prestigious evening of classical and contemporary ballet performed by the National Dance Company.",
                StartTime = new DateTime(2026, 5, 30, 19, 0, 0),
                EndTime = new DateTime(2026, 5, 30, 21, 30, 0),
                Venue = "City Theater",
                TotalSeats = 350,
                AvailableSeats = 173,
                Price = 90.00m,
                EventType = "Theater",
                ImageUrl = "https://images.unsplash.com/photo-1598899134739-24c46f58b8c0?w=800&q=80",
            },
            // ── June 2026 ─────────────────────────────────────────────────────────
            new Event
            {
                Id = 16,
                Title = "Street Food Carnival",
                Description = "70+ street food vendors from 30 countries. Flavours, music, and fun for the whole family.",
                StartTime = new DateTime(2026, 6, 1, 11, 0, 0),
                EndTime = new DateTime(2026, 6, 1, 21, 0, 0),
                Venue = "Central Square",
                TotalSeats = 2000,
                AvailableSeats = 1532,
                Price = 10.00m,
                EventType = "Food",
                ImageUrl = "https://images.unsplash.com/photo-1504674900247-0877df9cc836?w=800&q=80",
            },
            new Event
            {
                Id = 17,
                Title = "Tennis Open",
                Description = "City-wide tennis open tournament. Singles and doubles brackets for all skill levels. Spectator entry included.",
                StartTime = new DateTime(2026, 6, 3, 10, 0, 0),
                EndTime = new DateTime(2026, 6, 3, 18, 0, 0),
                Venue = "Sports Complex",
                TotalSeats = 1500,
                AvailableSeats = 789,
                Price = 40.00m,
                EventType = "Sports",
                ImageUrl = "https://images.unsplash.com/photo-1461896836934-ffe607ba8211?w=800&q=80",
            },
            new Event
            {
                Id = 18,
                Title = "Indie Film Screening",
                Description = "Festival-circuit award-winning indie films screened back to back with Q&A sessions with the directors.",
                StartTime = new DateTime(2026, 6, 5, 19, 0, 0),
                EndTime = new DateTime(2026, 6, 5, 23, 0, 0),
                Venue = "Cinema House",
                TotalSeats = 200,
                AvailableSeats = 118,
                Price = 22.00m,
                EventType = "Art",
                ImageUrl = "https://images.unsplash.com/photo-1536924430914-91f9e2041b83?w=800&q=80",
            },
            new Event
            {
                Id = 19,
                Title = "Jazz Brunch",
                Description = "Lazy Sunday brunch with live jazz quartet. Three-course brunch menu with bottomless mimosas included.",
                StartTime = new DateTime(2026, 6, 7, 11, 0, 0),
                EndTime = new DateTime(2026, 6, 7, 14, 0, 0),
                Venue = "Rooftop Lounge",
                TotalSeats = 60,
                AvailableSeats = 14,
                Price = 45.00m,
                EventType = "Music",
                ImageUrl = "https://images.unsplash.com/photo-1548163111-bc419d75fef4?w=800&q=80",
            },
            new Event
            {
                Id = 20,
                Title = "Web Dev Bootcamp",
                Description = "Intensive one-day bootcamp. Build and deploy a full-stack web app from scratch. Beginners welcome.",
                StartTime = new DateTime(2026, 6, 8, 9, 0, 0),
                EndTime = new DateTime(2026, 6, 8, 17, 0, 0),
                Venue = "Tech Hub",
                TotalSeats = 40,
                AvailableSeats = 11,
                Price = 299.00m,
                EventType = "Tech",
                ImageUrl = "https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=800&q=80",
            },
            new Event
            {
                Id = 21,
                Title = "Comedy Marathon",
                Description = "Four hours of non-stop stand-up comedy. 12 comedians, rotating every 20 minutes. Your cheeks will hurt.",
                StartTime = new DateTime(2026, 6, 12, 19, 0, 0),
                EndTime = new DateTime(2026, 6, 12, 23, 0, 0),
                Venue = "Comedy Palace",
                TotalSeats = 300,
                AvailableSeats = 201,
                Price = 28.00m,
                EventType = "Comedy",
                ImageUrl = "https://images.unsplash.com/photo-1527224857830-43a7acc85260?w=800&q=80",
            },
            new Event
            {
                Id = 22,
                Title = "Salsa Dance Night",
                Description = "Latin beats, professional instructors, and a packed dance floor. Beginners' crash course at 7pm before the social.",
                StartTime = new DateTime(2026, 6, 13, 20, 0, 0),
                EndTime = new DateTime(2026, 6, 13, 23, 0, 0),
                Venue = "Latin Club",
                TotalSeats = 150,
                AvailableSeats = 63,
                Price = 30.00m,
                EventType = "Music",
                ImageUrl = "https://images.unsplash.com/photo-1504609813442-a8924e83f76e?w=800&q=80",
            },
            new Event
            {
                Id = 23,
                Title = "Blockchain Summit",
                Description = "Deep dives into DeFi, Web3 infrastructure, and enterprise blockchain. Networking dinner included.",
                StartTime = new DateTime(2026, 6, 14, 9, 0, 0),
                EndTime = new DateTime(2026, 6, 14, 18, 0, 0),
                Venue = "Conference Center",
                TotalSeats = 200,
                AvailableSeats = 88,
                Price = 249.00m,
                EventType = "Business",
                ImageUrl = "https://images.unsplash.com/photo-1542744173-8e7e53415bb0?w=800&q=80",
            },
            // ── Conflict pair C: Outdoor Cinema vs Pop-Up Art Market (Jun 15) ──
            new Event
            {
                Id = 24,
                Title = "Outdoor Cinema Night",
                Description = "Classic films under the stars. Bring a blanket, grab a cocktail from the bar, and enjoy cinema the way it was meant to be.",
                StartTime = new DateTime(2026, 6, 15, 20, 0, 0),
                EndTime = new DateTime(2026, 6, 15, 23, 0, 0),
                Venue = "Rooftop Garden",
                TotalSeats = 200,
                AvailableSeats = 144,
                Price = 18.00m,
                EventType = "Art",
                ImageUrl = "https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=800&q=80",
            },
            new Event
            {
                Id = 25,
                Title = "Evening Art Market",
                Description = "Pop-up art market showcasing 40 local artists. Original paintings, prints, ceramics, and live art demonstrations.",
                StartTime = new DateTime(2026, 6, 15, 18, 0, 0),
                EndTime = new DateTime(2026, 6, 15, 22, 0, 0),
                Venue = "Warehouse District",
                TotalSeats = 500,
                AvailableSeats = 322,
                Price = 5.00m,
                EventType = "Art",
                ImageUrl = "https://images.unsplash.com/photo-1579783900882-c0d3dad7b119?w=800&q=80",
            },
            new Event
            {
                Id = 26,
                Title = "Summer Rock Fest",
                Description = "The biggest outdoor music festival of the summer. Eight bands across two stages, food trucks, and fireworks finale.",
                StartTime = new DateTime(2026, 6, 19, 16, 0, 0),
                EndTime = new DateTime(2026, 6, 19, 23, 0, 0),
                Venue = "Lakeside Amphitheatre",
                TotalSeats = 8000,
                AvailableSeats = 3211,
                Price = 85.00m,
                EventType = "Music",
                ImageUrl = "https://images.unsplash.com/photo-1493225457124-a3eb161ffa5f?w=800&q=80",
            },
            new Event
            {
                Id = 27,
                Title = "Food Truck Rally",
                Description = "30 food trucks, craft beers, and live acoustic music. Vote for your favourite truck and win prizes.",
                StartTime = new DateTime(2026, 6, 21, 12, 0, 0),
                EndTime = new DateTime(2026, 6, 21, 20, 0, 0),
                Venue = "Waterfront Plaza",
                TotalSeats = 3000,
                AvailableSeats = 2455,
                Price = 8.00m,
                EventType = "Food",
                ImageUrl = "https://images.unsplash.com/photo-1565123409695-7b5ef63a2efb?w=800&q=80",
            },
            new Event
            {
                Id = 28,
                Title = "Theater Night: Hamlet",
                Description = "A modern retelling of Shakespeare's Hamlet set in a near-future dystopia. Critically acclaimed production.",
                StartTime = new DateTime(2026, 6, 26, 19, 0, 0),
                EndTime = new DateTime(2026, 6, 26, 21, 30, 0),
                Venue = "Civic Theatre",
                TotalSeats = 300,
                AvailableSeats = 198,
                Price = 65.00m,
                EventType = "Theater",
                ImageUrl = "https://images.unsplash.com/photo-1507676184212-d03ab07a01bf?w=800&q=80",
            },
            new Event
            {
                Id = 29,
                Title = "Summer Jazz Concert",
                Description = "Al fresco jazz concert in the park. Bring a picnic, bring friends, and let the music carry the evening.",
                StartTime = new DateTime(2026, 6, 28, 18, 0, 0),
                EndTime = new DateTime(2026, 6, 28, 21, 0, 0),
                Venue = "Park Amphitheatre",
                TotalSeats = 1000,
                AvailableSeats = 567,
                Price = 35.00m,
                EventType = "Music",
                ImageUrl = "https://images.unsplash.com/photo-1548163111-bc419d75fef4?w=800&q=80",
            },
            // ── Conflict with Jazz Night (Id=1, May 2 19:00–22:00) ───────────────
            new Event
            {
                Id = 30,
                Title = "Blues & Soul Evening",
                Description = "An electric night of blues and soul with back-to-back sets from four acclaimed artists. Late bar, warm vibes, unforgettable music.",
                StartTime = new DateTime(2026, 5, 2, 20, 30, 0),
                EndTime = new DateTime(2026, 5, 2, 23, 0, 0),
                Venue = "The Rusty String",
                TotalSeats = 150,
                AvailableSeats = 92,
                Price = 20.00m,
                EventType = "Music",
                ImageUrl = "https://images.unsplash.com/photo-1516280440614-37939bbacd81?w=800&q=80",
            }
        );
    }
}
