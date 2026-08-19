using Cogitatio.General;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages;

public partial class Index
{
    private const int DefaultMonthsToShow = 12;

    [Inject] private SiteSettings SiteSettings { get; set; } = default!;
    [Inject] private ILogger<Index> logger { get; set; }

    [Inject] private IDatabase db { get; set; } = default!;

    private List<BlogPost> RecentPosts { get; set; } = new();

    private string SearchFurtherBackUrl { get; set; } = string.Empty;

    protected override Task OnInitializedAsync()
    {
        int monthsToShow = db.GetIntSetting(BlogSettings.HomeMonthsToShow, DefaultMonthsToShow);

        DateTime homeCutoff = DateTime.Now.AddMonths(-monthsToShow).Date;

        logger.LogDebug($"Getting posts published since {homeCutoff}");
        RecentPosts = db.GetPostsSince(homeCutoff);

        Dictionary<int, List<string>> tagsByPostId = db.GetTagsForPostsSince(homeCutoff);
        foreach (BlogPost post in RecentPosts)
        {
            post.Tags = tagsByPostId.TryGetValue(post.Id, out List<string> tags) ? tags : new List<string>();
        }

        DateTime priorWindowEnd = homeCutoff.AddDays(-1);
        DateTime priorWindowStart = DateTime.Now.AddMonths(-monthsToShow * 2).Date;
        SearchFurtherBackUrl = $"/search/{priorWindowStart:yyyy-MM-dd}/{priorWindowEnd:yyyy-MM-dd}";

        return Task.CompletedTask;
    }
}
