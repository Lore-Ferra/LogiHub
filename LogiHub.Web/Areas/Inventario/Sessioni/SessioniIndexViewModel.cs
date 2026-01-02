using LogiHub.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace LogiHub.Web.Areas.Inventario
{
    public class SessioniIndexViewModel : PagingViewModel
    {        
        public string Filter { get; set; }
        public override IActionResult GetRoute()
        {
            return MVC.Inventario.Sessioni
                .Index(Filter, Page, PageSize);
        }
    }
}