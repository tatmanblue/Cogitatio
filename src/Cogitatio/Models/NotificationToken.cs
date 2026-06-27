namespace Cogitatio.Models;

public class NotificationToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TenantId { get; set; }
    public string Token { get; set; } = string.Empty;
    public NotificationTokenType TokenType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UsedAt { get; set; }

    public static NotificationToken Create(int userId, int tenantId, NotificationTokenType tokenType)
    {
        return new NotificationToken
        {
            UserId = userId,
            TenantId = tenantId,
            Token = Guid.NewGuid().ToString("N"),
            TokenType = tokenType,
            CreatedAt = DateTime.UtcNow
        };
    }
}
