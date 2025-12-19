using LogiHub.Services.Shared;
using LogiHub.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections;
using System.Collections.Generic;
using LogiHub.Services.Shared.SemiLavorati;

namespace LogiHub.Web.Areas.Magazzino.Models
{
    public class SemiLavoratiIndexViewModel : PagingViewModel
    {
        public IEnumerable<SemiLavoratiIndexDTO.RigaSemiLavorato> SemiLavorati { get; set; }
        public string Filter { get; set; }

        public override IActionResult GetRoute()
        {
            return MVC.Magazzino.SemiLavorati
                .Index(Filter, Page, PageSize)
                .GetAwaiter()
                .GetResult();
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