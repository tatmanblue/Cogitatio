namespace Cogitatio.Models;

/// <summary>
/// For main admin account state tracking.
/// TODO can we use this for blog users too?
/// </summary>
public class UserState
{
    private ILogger<UserState> logger;
    public bool IsAdmin { get; set; } = false;
    public Guid InstanceId { get; } = Guid.NewGuid();

    public UserState(ILogger<UserState> logger)
    {
        this.logger = logger;
        logger.LogInformation($"UserState initialized: {InstanceId}");
    }
}