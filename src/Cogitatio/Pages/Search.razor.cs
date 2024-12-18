using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Pages;

public partial class Search : ComponentBase
{
    public enum SearchRouteTypes
    {
        Post,
        AdminEdit
    }
    
    [Inject] private ILogger<Search> logger { get; set; }
    [Inject] private IDatabase database { get; set; }
    
    [Parameter] public string? Tag { get; set; }

    [Parameter] public DateTime? StartDate { get; set; }

    [Parameter] public DateTime? EndDate { get; set; }
    [Parameter] public string ReturnTo { get; set; }

    private List<BlogPost> blogResults { get; set; } = new ();
    private List<string> topTags { get; set; } = new ();
    private string? selectedTag;
    private string? resultMessage = "No results found";
    private DateTime? selectedStartDate;
    private DateTime? selectedEndDate;

    protected override void OnParametersSet()
    {
        if (!string.IsNullOrEmpty(Tag))
        {
            selectedTag = Tag;
            SearchByTag();
        }
        else if (StartDate.HasValue && EndDate.HasValue)
        {
            selectedStartDate = StartDate;
            selectedEndDate = EndDate;
            SearchByDateRange();
        }
        else
        {
            ShowLastPosts();
        }

        ReturnTo = GetCorrectUrlPath();
        
        if (0 == topTags.Count)
            topTags = database.GetTopTags();
    }

    /// <summary>
    /// there are only two valid values for ReturnTo as the value
    /// is used to build a route:  either post (for reading a post)
    /// or adminedit (for editing a post)
    /// </summary>
    /// <returns></returns>
    private string GetCorrectUrlPath()
    {
        if (string.IsNullOrEmpty(ReturnTo))
            return "post";
        
        if ("admineditpost" == ReturnTo.ToLower())
            return "AdminEditPost";
        
        return "post";
    }

    private void SearchByTag()
    {
        // Fetch or filter results based on the Tag
        logger.LogDebug($"Searching by tag: {selectedTag}");
        blogResults = database.GetAllPostsByTag(selectedTag!);
        resultMessage = $"Found {blogResults.Count} post(s) by tag: {selectedTag}";
    }

    private void SearchByDateRange()
    {
        DateTime startDate = selectedStartDate!.Value.Date;
        DateTime endDate = selectedEndDate!.Value.Date.AddDays(1);
        logger.LogDebug($"Searching by date: {startDate}-{endDate}");
        blogResults = database.GetAllPostsByDates(startDate, endDate);
        resultMessage = $"Found {blogResults.Count} post(s)";
    }

    private void ShowLastPosts()
    {
        blogResults = database.GetRecentPosts();
        resultMessage = $"Most recent posts:";
    }

    private void ClearSearch()
    {
        selectedTag = null;
        selectedStartDate = null;
        selectedEndDate = null;
        blogResults.Clear();
        ShowLastPosts();
    }    
}