using System;
using System.Collections.Generic;
using LogiHub.Web.Features.SearchCard;
using LogiHub.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace LogiHub.Web.Areas.Inventari.Sessioni.Discrepanze;

public class DiscrepanzeViewModel : PagingViewModel
{
    
    public List<DiscrepanzaDTO> DaAggiungere { get; set; } = new();
    public List<DiscrepanzaDTO> DaRimuovere { get; set; } = new(); 
    public List<DiscrepanzaDTO> DaSpostare { get; set; } = new(); 
    public string NomeSessione { get; set; }
    public Guid SessioneId { get; set; }
    public bool IsSolaLettura { get; set; }
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