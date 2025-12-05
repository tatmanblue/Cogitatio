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

    [Parameter] public EventCallback<string> PasswordChanged { get; set; }
    [Parameter] public bool ShowStrengthMeter { get; set; } = true;
    
    private string passwordInputType = "password";
    private string passwordToggleIcon = "bi bi-eye-slash";
    private int passwordStrength = 0;
    private string passwordStrengthLabel = "";

    protected override void OnParametersSet()
    {
        (passwordStrength, passwordStrengthLabel) = Logic.Password.EvaluatePasswordStrength(Password);
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