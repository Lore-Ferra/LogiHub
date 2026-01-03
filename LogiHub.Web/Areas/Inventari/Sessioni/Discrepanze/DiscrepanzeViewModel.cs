using System;
using System.Collections.Generic;
using LogiHub.Services.Inventari.Sessioni.DTO;
using LogiHub.Web.Features.SearchCard;
using LogiHub.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace LogiHub.Web.Areas.Inventari.Sessioni.Discrepanze;

public class DiscrepanzeViewModel : PagingViewModel
{
    public Guid SessioneId { get; set; }
    public SearchCardViewModel SearchCard { get; set; }
    public IEnumerable<DiscrepanzaDTO> Discrepanze { get; set; }

    public override IActionResult GetRoute()
    {
        return MVC.Inventari.Discrepanze.Index(
            SessioneId,
            SearchCard.Query,
            Page,
            PageSize
        ).GetAwaiter().GetResult();
    }
}