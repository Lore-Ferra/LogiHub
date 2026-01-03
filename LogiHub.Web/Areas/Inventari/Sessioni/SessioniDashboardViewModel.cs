using LogiHub.Services.Inventari.Sessioni.DTO;
using LogiHub.Web.Features.SearchCard;
using LogiHub.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace LogiHub.Web.Areas.Inventari.Sessioni;

public class SessioniDashboardViewModel: PagingViewModel
{
    public SearchCardViewModel SearchCard { get; set; }
    public Guid SessioneId { get; set; }
    public string NomeSessione { get; set; }
    public bool Chiuso { get; set; }
    public string Filter { get; set; }
    public string SearchQuery { get; set; }
    public IEnumerable<SessioneDashboardDTO.UbicazioneConStato> UbicazioniFiltrate { get; set; }
    public override IActionResult GetRoute()
    {
        return MVC.Inventari.Sessioni
            .Dashboard(
                SessioneId,
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
