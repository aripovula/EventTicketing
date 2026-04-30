using EventTicketing.Api.Models;
using EventTicketing.Api.Services;
using Microsoft.AspNetCore.Identity;

namespace EventTicketing.Api.Data;

public static class UserSeeder
{
    // use DefaultCardNumber because it is a demo app
    private const string DefaultCardNumber = "4242424242424242";
    private const string DefaultExpiry = "12/32";

    public static void Seed(AppDbContext db, string cardEncryptionKey)
    {
        if (db.Users.Any()) return;

        var hasher = new PasswordHasher<User>();

        var users = new[]
        {
            new User { Name = "John Doe",     Email = "john@example.com",  Role = "user"  },
            new User { Name = "Jane Doer",    Email = "jane@example.com",  Role = "user"  },
            new User { Name = "Alex Johnson", Email = "alex@example.com",  Role = "user"  },
            new User { Name = "Admin",        Email = "admin@example.com", Role = "admin" },
        };

        foreach (var user in users)
        {
            user.PasswordHash = hasher.HashPassword(user, "Password");
            user.Cards.Add(new Card
            {
                EncryptedNumber = CardEncryptionService.Encrypt(DefaultCardNumber, cardEncryptionKey),
                Last4 = DefaultCardNumber[^4..],
                ExpiryDate = DefaultExpiry,
                CardType = CardEncryptionService.DetectCardType(DefaultCardNumber),
                IsDefault = true,
            });
        }

        db.Users.AddRange(users);
        db.SaveChanges();

        // Seed orders for regular users so conflict detection is visible in the event list.
        // John and Jane both hold a ticket for Jazz Night (Id=1, May 2 19:00–22:00),
        // which conflicts with Blues & Soul Evening (Id=30, May 2 20:30–23:00).
        var john = db.Users.First(u => u.Email == "john@example.com");
        var jane = db.Users.First(u => u.Email == "jane@example.com");

        db.Orders.AddRange(
            new Order { EventId = 1, UserId = john.Id, Email = john.Email, Price = 25.00m, BookedAt = new DateTime(2026, 4, 10, 9, 0, 0) },
            new Order { EventId = 1, UserId = jane.Id, Email = jane.Email, Price = 25.00m, BookedAt = new DateTime(2026, 4, 11, 10, 0, 0) }
        );
        db.SaveChanges();
    }
}
