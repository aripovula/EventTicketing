using System.Security.Cryptography;
using EventTicketing.Api.Services;

namespace EventTicketing.Tests.Services;

public class CardEncryptionServiceTests
{
    private const string Key = "test-encryption-key";
    private const string CardNumber = "4242424242424242";

    // Encrypt / Decrypt round-trip

    [Fact]
    public void Decrypt_ReturnsOriginalCardNumber_AfterEncrypt()
    {
        var encrypted = CardEncryptionService.Encrypt(CardNumber, Key);
        var decrypted = CardEncryptionService.Decrypt(encrypted, Key);

        Assert.Equal(CardNumber, decrypted);
    }

    [Fact]
    public void Encrypt_ProducesValidBase64()
    {
        var encrypted = CardEncryptionService.Encrypt(CardNumber, Key);

        var bytes = Convert.FromBase64String(encrypted); // throws if invalid
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void Encrypt_ProducesDifferentCiphertextEachCall()
    {
        // Each call generates a new random IV, so the output must differ
        var first = CardEncryptionService.Encrypt(CardNumber, Key);
        var second = CardEncryptionService.Encrypt(CardNumber, Key);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Decrypt_WithDifferentKey_DoesNotReturnOriginalValue()
    {
        // AES-CBC with PKCS7: wrong key either corrupts the padding (CryptographicException)
        // or produces garbage plaintext — either outcome confirms the ciphertext is unreadable
        var encrypted = CardEncryptionService.Encrypt(CardNumber, Key);

        try
        {
            var decrypted = CardEncryptionService.Decrypt(encrypted, "wrong-key");
            Assert.NotEqual(CardNumber, decrypted);
        }
        catch (CryptographicException)
        {
            // Padding error with wrong key is equally correct
        }
    }

    [Fact]
    public void Encrypt_WorksWithAnyStringKey()
    {
        // Key derivation via SHA-256 means any string is valid
        var encrypted = CardEncryptionService.Encrypt(CardNumber, "short");
        var decrypted = CardEncryptionService.Decrypt(encrypted, "short");

        Assert.Equal(CardNumber, decrypted);
    }

    [Fact]
    public void EncryptedValue_IsPaddedLongerThanPlaintext()
    {
        var encrypted = CardEncryptionService.Encrypt(CardNumber, Key);

        // At minimum: 16 bytes IV + at least one AES block (16 bytes) → 32 bytes → 44 base64 chars
        Assert.True(encrypted.Length > CardNumber.Length);
    }

    // DetectCardType

    [Theory]
    [InlineData("4242424242424242", "Visa")]
    [InlineData("4000000000000000", "Visa")]
    public void DetectCardType_ReturnsVisa_ForCardsStartingWith4(string number, string expected)
    {
        Assert.Equal(expected, CardEncryptionService.DetectCardType(number));
    }

    [Theory]
    [InlineData("5555555555554444", "Mastercard")]
    [InlineData("5100000000000000", "Mastercard")]
    public void DetectCardType_ReturnsMastercard_ForCardsStartingWith5(string number, string expected)
    {
        Assert.Equal(expected, CardEncryptionService.DetectCardType(number));
    }

    [Theory]
    [InlineData("378282246310005", "Amex")]
    [InlineData("340000000000000", "Amex")]
    public void DetectCardType_ReturnsAmex_ForCardsStartingWith3(string number, string expected)
    {
        Assert.Equal(expected, CardEncryptionService.DetectCardType(number));
    }

    [Theory]
    [InlineData("6011111111111117", "Discover")]
    [InlineData("6000000000000000", "Discover")]
    public void DetectCardType_ReturnsDiscover_ForCardsStartingWith6(string number, string expected)
    {
        Assert.Equal(expected, CardEncryptionService.DetectCardType(number));
    }

    [Fact]
    public void DetectCardType_ReturnsUnknown_ForUnrecognisedPrefix()
    {
        Assert.Equal("Unknown", CardEncryptionService.DetectCardType("9000000000000000"));
    }

    [Fact]
    public void DetectCardType_ReturnsUnknown_ForEmptyString()
    {
        Assert.Equal("Unknown", CardEncryptionService.DetectCardType(""));
    }
}
