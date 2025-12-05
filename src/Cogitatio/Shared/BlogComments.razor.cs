using Microsoft.AspNetCore.Components;
using Cogitatio.Interfaces;
using Cogitatio.Logic;
using Cogitatio.Models;

namespace Cogitatio.Shared;

public partial class BlogComments : ComponentBase
{
    [Inject] private ILogger<BlogComments> logger { get; set; }
    [Inject] private IDatabase db { get; set; }
    [Inject] private BlogUserState userState { get; set; }
    
    [Parameter] public BlogPost? PostContent { get; set; }
    
    private bool allowComments = false;

    protected override void OnInitialized()
    {
        allowComments = db.GetSettingAsBool(BlogSettings.AllowComments);
    }
}