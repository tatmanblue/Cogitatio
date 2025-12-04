namespace Cogitatio.Models;

/// <summary>
/// TODO can we combine this with AdminUserState?
/// For blog user account state tracking.
/// </summary>
public class BlogUserState
{
    public Guid InstanceId { get; } = Guid.NewGuid();
    public UserAccountStates AccountState { get; set; } = UserAccountStates.Unknown;
    public DateTime LastLogin { get; set; } = DateTime.MinValue;
}