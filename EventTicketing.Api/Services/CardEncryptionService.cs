using System.Security.Cryptography;
using System.Text;

namespace EventTicketing.Api.Services;

public static class CardEncryptionService
{
    // Derives a 256-bit AES key from any string via SHA-256
    private static byte[] DeriveKey(string keyString) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(keyString));

    public static string Encrypt(string cardNumber, string keyString)
    {
        using var aes = Aes.Create();
        aes.Key = DeriveKey(keyString);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(cardNumber);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend the 16-byte IV so we can recover it on decryption
        var result = new byte[16 + cipherBytes.Length];
        aes.IV.CopyTo(result, 0);
        cipherBytes.CopyTo(result, 16);
        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string encryptedValue, string keyString)
    {
        var allBytes = Convert.FromBase64String(encryptedValue);
        using var aes = Aes.Create();
        aes.Key = DeriveKey(keyString);
        aes.IV = allBytes[..16];

        using var decryptor = aes.CreateDecryptor();
        var cipherBytes = allBytes[16..];
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }

    public static string DetectCardType(string cardNumber) =>
        cardNumber.FirstOrDefault() switch
        {
            '4' => "Visa",
            '5' => "Mastercard",
            '3' => "Amex",
            '6' => "Discover",
            _ => "Unknown"
        };
}
