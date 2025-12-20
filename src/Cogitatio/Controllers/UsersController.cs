using Cogitatio.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Cogitatio.Controllers;

[Route("/api/users")]
[ApiController]
public class UsersController(ILogger<UsersController> logger) : ControllerBase
{
    /// <summary>
    /// For the proof of work sign-in process, get a random challenge string for the user to hash with a nonce.
    /// </summary>
    /// <returns></returns>
    [HttpGet("challenge")] 
    [EnableRateLimiting("user-access-policy")]
    public async Task<IResult> GetChallenge()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress;
        logger.LogInformation("Getting challenge for IP: {IpAddress}", ipAddress?.ToString());
        
        var bytes = new byte[32];
        Random.Shared.NextBytes(bytes);
        var challenge = Convert.ToBase64String(bytes);
        return Results.Json(new { challenge});
    }
}