using System.Collections.Generic;
using LogiHub.Services.Inventari.Sessioni.DTO;
using LogiHub.Web.Features.SearchCard;
using LogiHub.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace LogiHub.Web.Areas.Inventari.Sessioni;

public class SessioniIndexViewModel : PagingViewModel
{
    public SearchCardViewModel SearchCard { get; set; }
    public string Filter { get; set; }
    public IEnumerable<SessioniIndexDTO.RigaSessione> Sessioni { get; set; }
    public override IActionResult GetRoute()
    {
        return MVC.Inventari.Sessioni
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