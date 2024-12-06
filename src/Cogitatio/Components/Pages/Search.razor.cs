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

    private List<BlogPost> results { get; set; } = new List<BlogPost>();
    private string? selectedTag;
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
    }

    private void SearchByTag()
    {
        // Fetch or filter results based on the Tag
        logger.LogInformation($"Searching by tag: {selectedTag}");
        results = database.GetAllPostsByTag(selectedTag!);
    }

    private void SearchByDateRange()
    {
        // Fetch or filter results based on the Date Range
        logger.LogInformation($"Searching by date: {selectedStartDate}-{selectedEndDate}");
        // results = database.GetAllPostsByDateRange(SelectedStartDate!.Value, SelectedEndDate!.Value);
    }

    private void ClearSearch()
    {
        logger.LogInformation($"Clearing search");
        selectedTag = null;
        selectedStartDate = null;
        selectedEndDate = null;
        results.Clear();
    }    
}