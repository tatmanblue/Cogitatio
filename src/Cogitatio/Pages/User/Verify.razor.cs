using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages.User;

/// <summary>
/// Used to verify a user's email address.
/// </summary>
public partial class Verify : ComponentBase
{
    [SupplyParameterFromQuery(Name = "vid")]
    public string? VerificationId { get; set; }

    [SupplyParameterFromQuery(Name = "email")]
    public string? Email { get; set; }
    
    [Inject] IUserDatabase userDatabase { get; set; }
    [Inject] ILogger<Verify> logger { get; set; }
    
    private string message = "Verifying...";

    protected override void OnParametersSet()
    {
        BlogUserRecord record = userDatabase.Load(Email);
        if (record == null) 
        {
            logger.LogError($"Verification failed: no user with email {Email} found.");
            message = "Invalid verification link.";
            return;
        }

        if (record.AccountState != UserAccountStates.Created)
        {
            logger.LogError($"Verification failed: user with email {Email} is in state {record.AccountState}.");
            message = "Invalid verification link.";
            return;
        }
        
        if (record.VerificationId != VerificationId)
        {
            logger.LogError($"Verification failed: verification ID mismatch for user with email {Email}.");
            message = "Invalid verification link.";
            return;
        }
        
        record.AccountState = UserAccountStates.AwaitingApproval;
        userDatabase.Save(record);
        message = "Email verified successfully. Your account is now awaiting approval.";
        
    }
}