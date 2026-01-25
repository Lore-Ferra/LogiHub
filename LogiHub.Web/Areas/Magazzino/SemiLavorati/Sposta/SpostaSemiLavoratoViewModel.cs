using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LogiHub.Web.Areas.Magazzino.SemiLavorati;

public class SpostaSemiLavoratoViewModel
{
    public Guid Id { get; set; }
    public string Barcode { get; set; }
    public string Descrizione { get; set; }
    public string UbicazioneAttuale { get; set; }
    
    public Guid? NuovaUbicazioneId { get; set; }
    public List<SelectListItem> UbicazioniList { get; set; } = new();
}