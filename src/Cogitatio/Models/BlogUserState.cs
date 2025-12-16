namespace Cogitatio.Models;

/// <summary>
/// TODO can we combine this with AdminUserState?
/// For blog user account state tracking.
/// </summary>
public class BlogUserState
{
    public Guid InstanceId { get; } = Guid.NewGuid();
    public int AccountId { get; set; } = 0;
    public string DisplayName { get; set; }  = string.Empty;
    public UserAccountStates AccountState { get; set; } = UserAccountStates.Unknown;
    public DateTime LastLogin { get; set; } = DateTime.MinValue;
    public string SignInChallenge { get; set; } = string.Empty;
}