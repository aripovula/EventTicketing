using EventTicketing.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Card> Cards => Set<Card>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>().HasData(
            new Event
            {
                Id = 1,
                Title = "Jazz Night",
                Description = "An evening of live jazz music.",
                Date = new DateTime(2026, 8, 15, 20, 0, 0),
                Venue = "Blue Note Club",
                TotalSeats = 100,
                AvailableSeats = 100,
                Price = 25.00m,
                ImageUrl = "https://images.unsplash.com/photo-1548163111-bc419d75fef4?w=800&q=80"
            },
            new Event
            {
                Id = 2,
                Title = "Tech Conference 2026",
                Description = "A full-day conference on modern software development.",
                Date = new DateTime(2026, 7, 10, 9, 0, 0),
                Venue = "City Convention Centre",
                TotalSeats = 500,
                AvailableSeats = 500,
                Price = 149.00m,
                ImageUrl = "https://images.unsplash.com/photo-1582192730841-2a682d7375f9?w=800&q=80"
            }
        );
    }
}
