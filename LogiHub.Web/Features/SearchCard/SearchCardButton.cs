using System.Collections.Generic;

namespace LogiHub.Web.Features.SearchCard
{
    public class SearchCardButton
    {
        public string Text { get; set; }
        public string CssClass { get; set; } = "btn-primary";
        public string Type { get; set; } = "button";
        public string IconClass { get; set; }
        public Dictionary<string, string> HtmlAttributes { get; set; } = new();
    }
}