using System;
using System.Collections.Generic;
using LogiHub.Services.Inventari.Sessioni.DTO;
using LogiHub.Web.Features.SearchCard;
using LogiHub.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace LogiHub.Web.Areas.Inventari.Sessioni.Discrepanze;

public class DiscrepanzeViewModel : PagingViewModel
{
    
    public List<DiscrepanzaDTO> DaAggiungere { get; set; } = new(); // Extra puri
    public List<DiscrepanzaDTO> DaRimuovere { get; set; } = new();  // Mancanti puri
    public List<DiscrepanzaDTO> DaSpostare { get; set; } = new();   // Mancante in A + Extra in B

    public Guid SessioneId { get; set; }
    public SearchCardViewModel SearchCard { get; set; }
    public IEnumerable<DiscrepanzaDTO> Discrepanze { get; set; }
    public string Filter { get; set; }
    public string SearchQuery { get; set; }

    public override IActionResult GetRoute()
    {
        return MVC.Inventari.Discrepanze.Index(
            SessioneId,
            SearchCard.Query,
            SearchCard.Filters,
            Page,
            PageSize
        ).GetAwaiter().GetResult();
    }
}