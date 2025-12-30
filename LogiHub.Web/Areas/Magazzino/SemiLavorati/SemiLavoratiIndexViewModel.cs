using LogiHub.Services.Shared;
using LogiHub.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections;
using System.Collections.Generic;
using LogiHub.Services.Shared.SemiLavorati;
using LogiHub.Web.Features.SearchCard;

namespace LogiHub.Web.Areas.Magazzino.Models
{
    public class SemiLavoratiIndexViewModel : PagingViewModel
    {
        public IEnumerable<SemiLavoratiIndexDTO.RigaSemiLavorato> SemiLavorati { get; set; }
        public SearchCardViewModel SearchCard { get; set; }

        public override IActionResult GetRoute()
        {
            return MVC.Magazzino.SemiLavorati
                .Index(
                    SearchCard.Query, 
                    SearchCard.Filters, 
                    Page, 
                    PageSize
                )
                .GetAwaiter()
                .GetResult();
        }

        public SemilavoratiIndexQuery ToQuery()
        {
            return new SemilavoratiIndexQuery
            {
                Uscito = SearchCard.Filters.Uscito,
                SearchInColumns = SearchCard.Filters.SearchInColumns,
                Page = Page,
                PageSize = PageSize
            };
        }
    }
}