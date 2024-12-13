using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Shared;

public partial class BlogPostFull : ComponentBase
{
    [Parameter] public int? PostId { get; set; }
    [Parameter] public string Slug { get; set; }

    [Parameter] public BlogPost? PostContent { get; set; }
    
}