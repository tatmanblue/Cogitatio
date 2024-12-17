namespace Cogitatio.Models;

/// <summary>
/// Truth is this is test to see what happens with the
/// SignalR drops
/// </summary>
public class Statistics(ILogger<Statistics> logger)
{
    public int AccessCount { get; private set; } = 0;
    
    public void PageVisted() => ++AccessCount;
}