using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages;

public partial class AdminEditPost : ComponentBase
{
    [Inject] private ILogger<AdminEditPost> logger { get; set; }
    [Inject] private NavigationManager navigationManager { get; set; }
    [Inject] private IDatabase database { get; set; }
    [Inject] private UserState userState { get; set; }
    [Parameter] public string Slug { get; set; }
    
    private string title = string.Empty;
    private string tags = string.Empty;
    private string content = "<b>New blog Post</b>";
    
    
    private Dictionary<string, object> editorConfig = new Dictionary<string, object>{
        { "menubar", true },
        { "plugins", "link image code" },
        { "toolbar", "undo redo | styleselect | forecolor | bold italic | alignleft aligncenter alignright alignjustify | outdent indent | link image | code" }
    };
    
    
    protected override void OnParametersSet()
    {
        if (!userState.IsAdmin)
            navigationManager.NavigateTo("/Admin");
        
        if (string.IsNullOrEmpty(Slug))
            navigationManager.NavigateTo("/search/ret=admineditpost");
    }
    
    private async Task Update()
    {
        logger.LogInformation("Updating blog post");
    }
}