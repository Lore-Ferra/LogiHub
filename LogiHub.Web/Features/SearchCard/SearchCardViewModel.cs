namespace LogiHub.Web.Features.SearchCard;

using System.Collections.Generic;

public class SearchCardViewModel
{
    public string Title { get; set; }

    public string Query { get; set; }
    public string Placeholder { get; set; } = "Cerca…";
    
    public SearchCardFiltersViewModel Filters { get; set; } = new();

    public List<SearchCardButton> HeaderButtons { get; set; } = new();

    public bool ShowFilters { get; set; } = true;
    
    public bool ShowUscitoFilter { get; set; } = true;
    public bool ShowSearchInColumns { get; set; } = true;
    
    public List<SearchInColumnOption> SearchInColumns { get; set; } = new();
}
