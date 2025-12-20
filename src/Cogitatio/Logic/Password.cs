using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Cogitatio.Logic;

/// <summary>
/// Password hashing and verification functions
/// </summary>
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
    
    /// <summary>
    /// compares password with static rules as to what makes a good password and returns strength indicators.
    /// </summary>
    /// <param name="password"></param>
    /// <param name="minLength"></param>
    /// <param name="maxLength"></param>
    /// <returns></returns>
    public static (int, string) EvaluatePasswordStrength(string password, int minLength = 6, int maxLength = 30)
    {
        if (string.IsNullOrEmpty(password))
            return (0, "Come on dude!");
    
        if (password.Length < minLength)
            return (0, $"Yikes! Try to get more than {minLength} characters.");
        
        int score = 0;
    
        // Length check
        if (password.Length >= 8)
            score++;
        if (password.Length >= 12)
            score++;
    
        // Contains uppercase letters
        if (password.Any(char.IsUpper))
            score++;
    
        // Contains lowercase letters
        if (password.Any(char.IsLower))
            score++;
    
        // Contains digits
        if (password.Any(char.IsDigit))
            score++;
    
        // Contains special characters
        if (password.Any(ch => !char.IsLetterOrDigit(ch)))
            score++;
    
        // Evaluate score
        string strengthWord = score switch
        {
            >= 6 => "Ft Knox Strong",
            5 => "Solid password",
            4 => "It's ok I guess",
            3 => "Script kiddy level",
            _ => "Dude! That's weak..."
        };
        
        return (score, strengthWord);
    }
}