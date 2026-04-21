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
    }
}
