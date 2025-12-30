using System.Collections.Generic;

namespace LogiHub.Services.Shared.SemiLavorati
{
    public class SemilavoratiIndexQuery
    {
        public string SearchText { get; set; }
        public TriState Uscito { get; set; } = TriState.All;
        public List<string> SearchInColumns { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}