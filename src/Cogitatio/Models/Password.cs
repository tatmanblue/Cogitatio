using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Cogitatio.Models;

public static class Password
{
    // Use PBKDF2 with HMAC-SHA256 (or SHA512), 600,000+ iterations recommended in 2025
    private const int SaltSize = 128 / 8;        // 128 bits
    private const int HashSize = 256 / 8;        // 256 bits
    private const int Iterations = 600_000;    // OWASP 2024+ recommendation for PBKDF2-HMAC-SHA256

    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    /// <summary>
    /// Hashes a password securely with a random salt using PBKDF2.
    /// Returns a string in the format: "v1:iterations:salt:hash"
    /// </summary>
    public static string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));

        // Generate random salt
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        // Hash the password with the salt
        byte[] hash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: Iterations,
            numBytesRequested: HashSize);

        // Format: version:iterations:salt:hash (base64 encoded)
        return $"v1:{Iterations}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// Verifies a password against previously hashed value
    /// </summary>
    public static bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
            return false;

        var parts = hashedPassword.Split(':');
        if (parts.Length != 4 || parts[0] != "v1")
            return false; // Unknown format

        int iterations = int.Parse(parts[1]);
        byte[] salt = Convert.FromBase64String(parts[2]);
        byte[] expectedHash = Convert.FromBase64String(parts[3]);

        byte[] actualHash = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: iterations,
            numBytesRequested: HashSize);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }    
}