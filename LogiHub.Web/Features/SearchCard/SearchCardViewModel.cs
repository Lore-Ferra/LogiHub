namespace LogiHub.Web.Features.SearchCard;

using System.Collections.Generic;

public enum SearchCardMessageType
{
    None,
    Success,
    Warning,
    Danger
}
public class SearchCardViewModel
{
    public string Title { get; set; }

    public string Query { get; set; }
    public string Placeholder { get; set; }
    public string BackUrl { get; set; }
    public SearchCardFiltersViewModel Filters { get; set; } = new();

    public List<SearchCardButton> HeaderButtons { get; set; } = new();

    public bool ShowFilters { get; set; } = true;
    
    public bool ShowUscitoFilter { get; set; } = true;
    public bool ShowSearchInColumns { get; set; } = true;

    public string DefaultSearchInColumnKey { get; set; }
    
    public List<SearchInColumnOption> SearchInColumns { get; set; } = new();
    
    public string AlertTitle { get; set; }
    public string Message { get; set; }
    public SearchCardMessageType MessageType { get; set; } = SearchCardMessageType.None;

    public string GetAlertClass() => MessageType switch
    {
        SearchCardMessageType.Success => "alert-success bg-success-subtle text-success-emphasis border-0 shadow-sm",
        SearchCardMessageType.Warning => "alert-warning bg-warning-subtle text-warning-emphasis border-0 shadow-sm",
        SearchCardMessageType.Danger => "alert-danger bg-danger-subtle text-danger-emphasis border-0 shadow-sm",
        _ => "alert-info bg-info-subtle text-info-emphasis border-0 shadow-sm"
    };

    public string GetAlertIcon() => MessageType switch
    {
        SearchCardMessageType.Success => "fa-check-circle",
        SearchCardMessageType.Danger => "fa-times-circle",
        SearchCardMessageType.Warning => "fa-exclamation-triangle",
        _ => "fa-info-circle"
    };
}

