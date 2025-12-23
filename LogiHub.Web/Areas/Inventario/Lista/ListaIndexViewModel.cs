using LogiHub.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace LogiHub.Web.Areas.Inventario
{
    public class ListaIndexViewModel : PagingViewModel
    {        
        public string Filter { get; set; }
        public override IActionResult GetRoute()
        {
            return MVC.Inventario.Lista
                .Index(Filter, Page, PageSize);
        }
    }
}