using LogiHub.Services.Shared;

namespace LogiHub.Web.Features.SearchCard
{
    using System.Collections.Generic;


    public class SearchCardFiltersViewModel
    {
        public TriState Uscito { get; set; } = TriState.All;

        public List<string> SearchInColumns { get; set; } = new();
    }
}