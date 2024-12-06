using Microsoft.AspNetCore.Components;

namespace Cogitatio.Components.Layout;

public partial class BlogTags : ComponentBase
{
    [Parameter] public List<string> Tags { get; set; } = new();
}