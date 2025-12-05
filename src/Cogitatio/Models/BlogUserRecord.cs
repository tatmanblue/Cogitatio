namespace Cogitatio.Models;

/// <summary>
/// Basically the DAO for the user data
/// </summary>
public class BlogUserRecord
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserAccountStates AccountState { get; set; } = UserAccountStates.Unknown;
    public string TwoFactorSecret { get; set; } = string.Empty;
    public string VerificationId { get; set; } = string.Empty;
    public string Password { get; set; }        // This password is always hashed
    public DateTime CreatedAt { get; set; }

}