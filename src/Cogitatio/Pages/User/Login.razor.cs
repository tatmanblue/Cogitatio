using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages.User;

public partial class Login : ComponentBase
{
    [Inject] SiteSettings site { get; set; }

    protected override void OnInitialized()
    {
        
    }
}