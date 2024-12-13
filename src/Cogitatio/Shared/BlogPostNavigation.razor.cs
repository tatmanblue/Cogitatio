using Microsoft.AspNetCore.Components;

namespace Cogitatio.Shared;

public partial class BlogPostNavigation : ComponentBase
{
    [Parameter] public string PreviousPostTitle { get; set; }
    [Parameter] public string PreviousPostSlug { get; set; }
    [Parameter] public string NextPostTitle { get; set; }
    [Parameter] public string NextPostSlug { get; set; }

}