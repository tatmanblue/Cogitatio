namespace Cogitatio.Models;

/// <summary>
/// For main admin account state tracking.
/// TODO can we use this for blog users too?
/// </summary>
public class AdminUserState
{
    private ILogger<AdminUserState> logger;
    public bool IsAdmin { get; set; } = false;
    public Guid InstanceId { get; } = Guid.NewGuid();

    public AdminUserState(ILogger<AdminUserState> logger)
    {
        this.logger = logger;
        logger.LogInformation($"UserState initialized: {InstanceId}");
    }
}