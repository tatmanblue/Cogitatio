using Cogitatio.General;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Cogitatio.Shared;

/// <summary>
/// For input of a password showing a password strength meter and show/hide toggle.
/// </summary>
public partial class PasswordEditor : ComponentBase
{
    [Parameter, EditorRequired]
    public string Password
    {
        get;
        set
        {
            if (!EqualityComparer<string>.Default.Equals(field, value))
            {
                field = value;
                (passwordStrength, passwordStrengthLabel) = Logic.Password.EvaluatePasswordStrength(Password);
                PasswordChanged.InvokeAsync(value);
            }
        }
    } = string.Empty;

    [Parameter] public int MinLength { get; set; } = 6;
    [Parameter] public int MaxLength { get; set; } = 30;
    [Parameter] public EventCallback<string> PasswordChanged { get; set; }
    [Parameter] public bool ShowStrengthMeter { get; set; } = true;
    
    private string uniqueId = "credential";
    private string passwordInputType = "password";
    private string passwordToggleIcon = "bi bi-eye-slash";
    private int passwordStrength = 0;
    private string passwordStrengthLabel = "";

    protected override void OnInitialized()
    {
        uniqueId = $"credential_{Guid.NewGuid().VerificationId()}"; 
    }
    
    protected override void OnParametersSet()
    {
        (passwordStrength, passwordStrengthLabel) = Logic.Password.EvaluatePasswordStrength(Password, MinLength, MaxLength);
    }
    
    private async Task OnPasswordChangedInternal(ChangeEventArgs e)
    {
        Password = e.Value?.ToString() ?? string.Empty;
        (passwordStrength, passwordStrengthLabel) = Logic.Password.EvaluatePasswordStrength(Password, MinLength, MaxLength);
        await PasswordChanged.InvokeAsync(Password);
    }
    
    private void TogglePasswordVisibility()
    {
        if (passwordInputType == "password")
        {
            passwordInputType = "text";
            passwordToggleIcon = "bi bi-eye"; // Eye icon for visible password
        }
        else
        {
            passwordInputType = "password";
            passwordToggleIcon = "bi bi-eye-slash"; // Eye-slash icon for hidden password
        }
    }
}