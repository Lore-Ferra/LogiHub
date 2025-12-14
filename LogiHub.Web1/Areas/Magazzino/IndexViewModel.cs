using LogiHub.Services.Shared;
using System.Collections;
using System.Collections.Generic;

namespace LogiHub.Web.Areas.Magazzino
{
    public class IndexViewModel
    {
        public IEnumerable<SemiLavoratiIndexDTO.RigaSemiLavorato> SemiLavorati { get; set; } = new List<SemiLavoratiIndexDTO.RigaSemiLavorato>();
        public string Filter { get; set; }
        public void SetData(SemiLavoratiIndexDTO dto)
        {
            SemiLavorati = dto.Items;
        }
        public SemilavoratiIndexQuery ToQuery()
        {
            return new SemilavoratiIndexQuery
            {
                Filter = this.Filter
            };
        }
    }
}
