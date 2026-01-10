using System;
using System.Collections.Generic;
using LogiHub.Services.Inventari.Sessioni.DTO;
using LogiHub.Web.Features.SearchCard;
using LogiHub.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace LogiHub.Web.Areas.Inventari.Sessioni;

public class RilevamentoUbicazioneViewModel : PagingViewModel
{
    public Guid SessioneId { get; set; }
    public Guid UbicazioneId { get; set; }
    public string NomeUbicazione { get; set; }

    public SearchCardViewModel SearchCard { get; set; }
    public IEnumerable<PezzoInventarioDTO> Pezzi { get; set; }
    public bool IsSolaLettura { get; set; }
    public int TotaliPezzi { get; set; }
    public int PezziRilevati { get; set; }
    public int ConteggioExtra { get; set; }
    
    public int PercentualeCompletamento => TotaliPezzi > 0 
        ? (int)((double)PezziRilevati / TotaliPezzi * 100) 
        : 0;

    public override IActionResult GetRoute()
    {
        return MVC.Inventari.RilevamentoUbicazione.Index(
            SessioneId,
            UbicazioneId,
            SearchCard.Query,
            Page,
            PageSize
        ).GetAwaiter().GetResult();
    }
}