using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Components.Layout;

public partial class BlogPostShort : ComponentBase
{
    [Parameter] public int? PostId { get; set; }
    [Parameter] public string Slug { get; set; }

    [Parameter] public BlogPost? PostContent { get; set; }
    
}