using Microsoft.AspNetCore.Components;

namespace Cogitatio.Shared;

public partial class BlogTags : ComponentBase
{
    [Parameter] public List<string> Tags { get; set; } = new();
    [Parameter] public string Subline { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (string.IsNullOrEmpty(Subline))
            Subline = "Tags";
    }
}