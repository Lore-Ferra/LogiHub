namespace LogiHub.Web.Features.SearchCard;

using System.Collections.Generic;

public class SearchCardViewModel
{
    public string Title { get; set; }
    public string InputName { get; set; } = "Filter";
    public string InputValue { get; set; }
    public string Placeholder { get; set; } = "Cerca…";
    public List<SearchCardButton> Buttons { get; set; } = new();
    public Dictionary<string, string> HiddenFields { get; set; } = new();
}
