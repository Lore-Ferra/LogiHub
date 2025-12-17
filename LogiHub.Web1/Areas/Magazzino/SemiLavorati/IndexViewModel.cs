using LogiHub.Services.Shared;
using System.Collections;
using System.Collections.Generic;

namespace LogiHub.Web.Areas.Magazzino.Models
{
    public class IndexViewModel
    {
        public IEnumerable<SemiLavoratiIndexDTO.RigaSemiLavorato> SemiLavorati { get; set; } = new List<SemiLavoratiIndexDTO.RigaSemiLavorato>();
        public string Filter { get; set; }
        
        // Proprietà per la paginazione
        public int PageIndex { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
        
        public SemilavoratiIndexQuery ToQuery()
        {
            return new SemilavoratiIndexQuery
            {
                Filter = this.Filter
            };
        }
    }
}
