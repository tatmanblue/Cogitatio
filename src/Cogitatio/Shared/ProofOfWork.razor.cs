using System.Security.Cryptography;
using System.Text;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Cogitatio.Shared;

/// <summary>
/// Proof of Work is browser side function to slow down access on page by forcing user to wait through
/// the generation of nonce from a challenge value provided by the blog.  
/// Verification the wait was performed is by verifying the results
/// </summary>
public partial class ProofOfWork : ComponentBase
{
    [Inject] IJSRuntime JS { get; set; } = null!;
    
    private string waitMessage = "Getting all the bits in a row...";        // TODO again like to make this configurabl
    private string progress = "starting…";
    private PoWResult powResult = null;
    private int powDifficulty = 21;                                         // TODO: make configurable
    private string challengeUrl = "/api/users/challenge";                   // TODO: make this configurable

    /// <summary>
    /// Call this method to trigger the work done in browser 
    /// </summary>
    /// <returns></returns>
    public async Task<PoWResult> Start()
    {
        powResult = await JS.InvokeAsync<PoWResult>("startProofOfWork", powDifficulty, challengeUrl);
        // TODO:  handle: catch (Microsoft.JSInterop.JSException ex)

        return powResult;
    }

    /// <summary>
    /// Used to verify the results returned from Start()
    /// </summary>
    /// <param name="challenge"></param>
    /// <param name="nonce"></param>
    /// <returns></returns>
    public bool Verify(PoWResult result)
    {
        return VerifyProofOfWork(result.Challenge, result.Nonce);
    }
    
    private bool VerifyProofOfWork(string challenge, long nonce)
    {
        /*
        // Prevent replay attacks
        if (!challenge.StartsWith(DateTime.UtcNow.ToString("yyyyMMddHH")))
            return false;
        */
        
        using var sha256 = SHA256.Create();
        var input = challenge + nonce;
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha256.ComputeHash(bytes);
    
        // Convert first 4 bytes to int (big-endian)
        uint hashValue = (uint)(
            (hashBytes[0] << 24) |
            (hashBytes[1] << 16) |
            (hashBytes[2] << 8)  |
            hashBytes[3]);

        // Difficulty 22 = need first 22 bits to be zero → hash < 2^(32-22) = 2^10 = 1024
        uint target = 1u << (32 - powDifficulty); // 1 << 10 = 1024
        return hashValue < target;
    }
}