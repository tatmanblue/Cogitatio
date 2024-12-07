using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.AspNetCore.Components;

namespace Cogitatio.Components.Pages;

public partial class Search : ComponentBase
{
    [Inject] private ILogger<Search> logger { get; set; }
    [Inject] private IDatabase database { get; set; }
    
    [Parameter]
    public string? Tag { get; set; }

    [Parameter]
    public DateTime? StartDate { get; set; }

    [Parameter]
    public DateTime? EndDate { get; set; }

    private List<BlogPost> results { get; set; } = new ();
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
    }

    private void SearchByTag()
    {
        // Fetch or filter results based on the Tag
        logger.LogDebug($"Searching by tag: {selectedTag}");
        results = database.GetAllPostsByTag(selectedTag!);
        resultMessage = $"Found {results.Count} post(s) by tag: {selectedTag}";
    }

    private void SearchByDateRange()
    {
        DateTime startDate = selectedStartDate!.Value.Date;
        DateTime endDate = selectedEndDate!.Value.Date.AddDays(1);
        logger.LogDebug($"Searching by date: {startDate}-{endDate}");
        results = database.GetAllPostsByDates(startDate, endDate);
        resultMessage = $"Found {results.Count} post(s)";
    }

    private void ShowLastPosts()
    {
        results = database.GetRecentPosts();
        resultMessage = $"Most recent posts:";
    }

    private void ClearSearch()
    {
        selectedTag = null;
        selectedStartDate = null;
        selectedEndDate = null;
        results.Clear();
        ShowLastPosts();
    }    
}