using LogiHub.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using LogiHub.Web.Features.SearchCard;

namespace LogiHub.Web.Areas.Inventario.Models;

public class SessioniIndexViewModel : PagingViewModel
{
    public SearchCardViewModel SearchCard { get; set; }
    public string Filter { get; set; }

    public override IActionResult GetRoute()
    {
        return MVC.Inventario.Sessioni
            .Index(
                SearchCard.Query,
                SearchCard.Filters,
                Page,
                PageSize
            )
            .GetAwaiter()
            .GetResult();
        ;
    }
}